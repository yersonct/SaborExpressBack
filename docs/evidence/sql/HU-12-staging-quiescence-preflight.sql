\set ON_ERROR_STOP on

-- Read-only guard for HU-12. Run in the same direct psql session immediately
-- before the authorized migration file. It scopes activity to neondb so Neon
-- monitoring sessions attached to another database do not block this migration.
-- The only same-database client exception is the verified, idle, read-only Neon
-- Auth role described below. Every other client backend is a blocker.
-- Invoke this file with session statement_timeout=30s; restore 15min only after
-- this guard succeeds and immediately before the migration file.
DO $hu12_guard$
DECLARE
    migration_count integer;
    latest_migration text;
    prepared_transaction_count integer;
    public_table_count integer;
    neon_auth_role_oid oid;
    neon_auth_schema_owned boolean := false;
    neon_auth_can_write boolean := true;
    additional_clients integer;
    eligible_auth_clients integer;
    blocking_session record;
    consecutive_clean_checks integer := 0;
    check_number integer := 0;
    deadline timestamptz;
BEGIN
    IF current_database() <> 'neondb' THEN
        RAISE EXCEPTION 'HU-12 guard aborted: unexpected database';
    END IF;

    IF current_user <> 'neondb_owner' THEN
        RAISE EXCEPTION 'HU-12 guard aborted: unexpected database role';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_roles WHERE rolname = current_user AND rolsuper
    ) AND NOT EXISTS (
        SELECT 1
          FROM pg_roles
         WHERE rolname = 'pg_read_all_stats'
           AND pg_has_role(current_user, oid, 'USAGE')
    ) THEN
        RAISE EXCEPTION 'HU-12 guard aborted: role cannot verify all PostgreSQL sessions';
    END IF;

    SELECT count(*), max("MigrationId")
      INTO migration_count, latest_migration
      FROM public."__EFMigrationsHistory";

    IF migration_count <> 35 OR latest_migration <> '20261022110000_EnforceNotificationIdempotency' THEN
        RAISE EXCEPTION 'HU-12 guard aborted: migration history is not the expected 35/36 state';
    END IF;

    IF to_regclass('public.employee_schedules') IS NOT NULL
       OR to_regclass('public."EmployeeSchedules"') IS NULL
       OR EXISTS (
            SELECT 1
              FROM information_schema.columns
             WHERE table_schema = 'public'
               AND table_name = 'customers'
               AND column_name = 'privacy_accepted_at_utc'
       ) THEN
        RAISE EXCEPTION 'HU-12 guard aborted: schema is not the expected pre-migration state';
    END IF;

    SELECT count(*)
      INTO prepared_transaction_count
      FROM pg_prepared_xacts;

    IF prepared_transaction_count <> 0 THEN
        RAISE EXCEPTION 'HU-12 guard aborted: a prepared transaction exists';
    END IF;

    SELECT count(*)
      INTO public_table_count
      FROM pg_class c
      JOIN pg_namespace n ON n.oid = c.relnamespace
     WHERE n.nspname = 'public'
       AND c.relkind IN ('r', 'p');

    IF public_table_count <> 34 THEN
        RAISE EXCEPTION 'HU-12 guard aborted: unexpected number of public tables';
    END IF;

    SELECT oid
      INTO neon_auth_role_oid
      FROM pg_roles
     WHERE rolname = 'neon_auth';

    IF neon_auth_role_oid IS NOT NULL THEN
        SELECT EXISTS (
            SELECT 1
              FROM pg_namespace n
             WHERE n.nspname = 'neon_auth'
               AND n.nspowner = neon_auth_role_oid
        )
          INTO neon_auth_schema_owned;

        WITH RECURSIVE role_chain(role_oid) AS (
            SELECT neon_auth_role_oid
            UNION
            SELECT am.roleid
              FROM pg_auth_members am
              JOIN role_chain rc ON am.member = rc.role_oid
        ),
        public_tables AS (
            SELECT c.oid
              FROM pg_class c
              JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE n.nspname = 'public'
               AND c.relkind IN ('r', 'p')
        ),
        write_privileges(privilege_name) AS (
            VALUES ('INSERT'), ('UPDATE'), ('DELETE'), ('TRUNCATE')
        ),
        role_capabilities AS (
            SELECT pr.rolsuper,
                   has_schema_privilege(pr.oid, 'public', 'CREATE') AS can_create_public,
                   EXISTS (
                       SELECT 1
                         FROM pg_proc p
                         JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname NOT IN ('pg_catalog', 'information_schema')
                          AND p.prosecdef
                          AND has_function_privilege(pr.oid, p.oid, 'EXECUTE')
                   ) AS can_execute_security_definer,
                   EXISTS (
                       SELECT 1
                         FROM public_tables pt
                         CROSS JOIN write_privileges wp
                        WHERE has_table_privilege(pr.oid, pt.oid, wp.privilege_name)
                           OR CASE
                                WHEN wp.privilege_name IN ('INSERT', 'UPDATE')
                                THEN has_any_column_privilege(pr.oid, pt.oid, wp.privilege_name)
                                ELSE false
                              END
                   ) AS can_write_public
              FROM role_chain rc
              JOIN pg_roles pr ON pr.oid = rc.role_oid
        )
        SELECT COALESCE(bool_or(rolsuper OR can_create_public OR can_execute_security_definer OR can_write_public), true)
          INTO neon_auth_can_write
          FROM role_capabilities;
    END IF;

    deadline := clock_timestamp() + interval '30 seconds';

    -- The polling window performs only this connection check. Activity in other
    -- databases (for example Neon cloud_admin monitoring on postgres) is outside
    -- this migration's database and does not block it. In neondb, either there are
    -- no other clients, or there is exactly one verified read-only Neon Auth
    -- PgBouncer session. Any other session/state/transaction aborts immediately.
    -- Two consecutive clean observations are needed before continuing.
    LOOP
        SELECT count(*)
          INTO additional_clients
          FROM pg_stat_activity
         WHERE datname = current_database()
           AND pid <> pg_backend_pid()
           AND backend_type = 'client backend';

        check_number := check_number + 1;

        IF additional_clients > 1 THEN
            FOR blocking_session IN
                SELECT pid,
                       COALESCE(usename, '<null>') AS observed_user,
                       CASE
                           WHEN application_name IS NULL THEN '<null>'
                           WHEN application_name ~* '(password|passwd|pwd|secret|token|api[_-]?key|authorization|bearer|credential|://|@)'
                             OR application_name ~ '^[[:alnum:]_+/=-]{32,}$'
                           THEN '<redacted-suspicious-app-name>'
                           ELSE left(regexp_replace(application_name, '[[:cntrl:]]+', ' ', 'g'), 120)
                       END AS session_application,
                       COALESCE(datname, '<null>') AS session_database,
                       COALESCE(state, '<null>') AS session_state,
                       COALESCE(client_addr::text, '<local-or-null>') AS session_address,
                       xact_start IS NOT NULL OR backend_xid IS NOT NULL AS has_open_transaction,
                       CASE
                           WHEN neon_auth_role_oid IS NOT NULL
                            AND neon_auth_schema_owned
                            AND NOT neon_auth_can_write
                            AND usename = 'neon_auth'
                            AND usesysid = neon_auth_role_oid
                            AND application_name = 'pgbouncer'
                            AND client_addr = inet '127.0.0.1'
                            AND state = 'idle'
                            AND xact_start IS NULL
                            AND backend_xid IS NULL
                            AND backend_xmin IS NULL
                           THEN 'verified_neon_auth_candidate'
                           ELSE 'blocking'
                       END AS session_classification
                  FROM pg_stat_activity
                 WHERE datname = current_database()
                   AND pid <> pg_backend_pid()
                   AND backend_type = 'client backend'
                 ORDER BY pid
            LOOP
                RAISE NOTICE 'HU-12 session diagnostic: pid=%, user=%, application=%, database=%, state=%, address=%, open_transaction=%, classification=%',
                    blocking_session.pid,
                    left(regexp_replace(blocking_session.observed_user, '[[:cntrl:]]+', ' ', 'g'), 120),
                    blocking_session.session_application,
                    left(regexp_replace(blocking_session.session_database, '[[:cntrl:]]+', ' ', 'g'), 120),
                    blocking_session.session_state,
                    blocking_session.session_address,
                    blocking_session.has_open_transaction,
                    blocking_session.session_classification;
            END LOOP;
            RAISE EXCEPTION 'HU-12 guard aborted: multiple additional client backends observed';
        ELSIF additional_clients = 1 THEN
            IF neon_auth_role_oid IS NULL
               OR NOT neon_auth_schema_owned
               OR neon_auth_can_write THEN
                FOR blocking_session IN
                    SELECT pid,
                           COALESCE(usename, '<null>') AS observed_user,
                           CASE
                               WHEN application_name IS NULL THEN '<null>'
                               WHEN application_name ~* '(password|passwd|pwd|secret|token|api[_-]?key|authorization|bearer|credential|://|@)'
                                 OR application_name ~ '^[[:alnum:]_+/=-]{32,}$'
                               THEN '<redacted-suspicious-app-name>'
                               ELSE left(regexp_replace(application_name, '[[:cntrl:]]+', ' ', 'g'), 120)
                           END AS session_application,
                           COALESCE(datname, '<null>') AS session_database,
                           COALESCE(state, '<null>') AS session_state,
                           COALESCE(client_addr::text, '<local-or-null>') AS session_address,
                           xact_start IS NOT NULL OR backend_xid IS NOT NULL AS has_open_transaction
                      FROM pg_stat_activity
                     WHERE datname = current_database()
                       AND pid <> pg_backend_pid()
                       AND backend_type = 'client backend'
                     ORDER BY pid
                LOOP
                    RAISE NOTICE 'HU-12 blocking session: pid=%, user=%, application=%, database=%, state=%, address=%, open_transaction=%',
                        blocking_session.pid,
                        left(regexp_replace(blocking_session.observed_user, '[[:cntrl:]]+', ' ', 'g'), 120),
                        blocking_session.session_application,
                        left(regexp_replace(blocking_session.session_database, '[[:cntrl:]]+', ' ', 'g'), 120),
                        blocking_session.session_state,
                        blocking_session.session_address,
                        blocking_session.has_open_transaction;
                END LOOP;
                RAISE EXCEPTION 'HU-12 guard aborted: Neon Auth identity or read-only privileges were not verified';
            END IF;

            SELECT count(*)
              INTO eligible_auth_clients
              FROM pg_stat_activity
             WHERE datname = current_database()
               AND pid <> pg_backend_pid()
               AND backend_type = 'client backend'
               AND usename = 'neon_auth'
               AND usesysid = neon_auth_role_oid
               AND application_name = 'pgbouncer'
               AND client_addr = inet '127.0.0.1'
               AND state = 'idle'
               AND xact_start IS NULL
               AND backend_xid IS NULL
               AND backend_xmin IS NULL;

            IF eligible_auth_clients <> 1 THEN
                FOR blocking_session IN
                    SELECT pid,
                           COALESCE(usename, '<null>') AS observed_user,
                           CASE
                               WHEN application_name IS NULL THEN '<null>'
                               WHEN application_name ~* '(password|passwd|pwd|secret|token|api[_-]?key|authorization|bearer|credential|://|@)'
                                 OR application_name ~ '^[[:alnum:]_+/=-]{32,}$'
                               THEN '<redacted-suspicious-app-name>'
                               ELSE left(regexp_replace(application_name, '[[:cntrl:]]+', ' ', 'g'), 120)
                           END AS session_application,
                           COALESCE(datname, '<null>') AS session_database,
                           COALESCE(state, '<null>') AS session_state,
                           COALESCE(client_addr::text, '<local-or-null>') AS session_address,
                           xact_start IS NOT NULL OR backend_xid IS NOT NULL AS has_open_transaction
                      FROM pg_stat_activity
                     WHERE datname = current_database()
                       AND pid <> pg_backend_pid()
                       AND backend_type = 'client backend'
                     ORDER BY pid
                LOOP
                    RAISE NOTICE 'HU-12 blocking session: pid=%, user=%, application=%, database=%, state=%, address=%, open_transaction=%',
                        blocking_session.pid,
                        left(regexp_replace(blocking_session.observed_user, '[[:cntrl:]]+', ' ', 'g'), 120),
                        blocking_session.session_application,
                        left(regexp_replace(blocking_session.session_database, '[[:cntrl:]]+', ' ', 'g'), 120),
                        blocking_session.session_state,
                        blocking_session.session_address,
                        blocking_session.has_open_transaction;
                END LOOP;
                RAISE EXCEPTION 'HU-12 guard aborted: additional client is not the single verified idle Neon Auth session';
            END IF;

            RAISE NOTICE 'HU-12 permits one verified idle Neon Auth session with no effective write privileges on public tables';
        END IF;

        consecutive_clean_checks := consecutive_clean_checks + 1;

        RAISE NOTICE 'HU-12 connection check %, additional client backends %, consecutive clean checks %',
            check_number, additional_clients, consecutive_clean_checks;

        IF consecutive_clean_checks >= 2 THEN
            RAISE NOTICE 'HU-12 connection preflight passed';
            EXIT;
        END IF;

        IF clock_timestamp() >= deadline THEN
            RAISE EXCEPTION 'HU-12 guard aborted: did not observe two consecutive clean checks within 30 seconds';
        END IF;

        PERFORM pg_sleep(1);
    END LOOP;
END
$hu12_guard$;
