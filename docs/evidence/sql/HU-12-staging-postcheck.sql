-- Read-only postcheck. An error here DOES NOT imply the DDL was rolled back.
DO $hu12_post$
DECLARE
  n integer;
  latest text;
BEGIN
  SELECT count(*), max("MigrationId") INTO n, latest
  FROM public."__EFMigrationsHistory";
  IF n <> 36 OR latest <> '20261030124239_AuditCorrections20261030' THEN
    RAISE EXCEPTION 'HU-12: postcheck failed (% migrations, latest %); DO NOT RETRY', n, latest;
  END IF;
  IF to_regclass('public.employee_schedules') IS NULL
    OR to_regclass('public."EmployeeSchedules"') IS NOT NULL
    OR NOT EXISTS (
      SELECT 1 FROM information_schema.columns
      WHERE table_schema='public' AND table_name='customers'
        AND column_name='privacy_accepted_at_utc'
    ) THEN
    RAISE EXCEPTION 'HU-12: postcheck failed (schema); DO NOT RETRY';
  END IF;
  RAISE NOTICE 'HU-12: PASS 36/36, latest %, key schema changes present', latest;
END
$hu12_post$;
