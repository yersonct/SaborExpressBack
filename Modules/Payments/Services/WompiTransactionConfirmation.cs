using SaborExpress.Modules.Payments.DTOs;

namespace SaborExpress.Modules.Payments.Services;

public static class WompiTransactionConfirmation
{
    public static bool MatchesAuthoritativeTransaction(
        WompiTransactionDto? webhookTransaction,
        WompiTransactionDto? authoritativeTransaction)
    {
        if (webhookTransaction == null || authoritativeTransaction == null ||
            string.IsNullOrWhiteSpace(webhookTransaction.Id) ||
            string.IsNullOrWhiteSpace(webhookTransaction.Reference) ||
            string.IsNullOrWhiteSpace(webhookTransaction.Status) ||
            webhookTransaction.AmountInCents <= 0)
            return false;

        return string.Equals(authoritativeTransaction.Id, webhookTransaction.Id, StringComparison.Ordinal) &&
               string.Equals(authoritativeTransaction.Reference, webhookTransaction.Reference, StringComparison.Ordinal) &&
               string.Equals(authoritativeTransaction.Status, webhookTransaction.Status, StringComparison.Ordinal) &&
               authoritativeTransaction.AmountInCents == webhookTransaction.AmountInCents;
    }
}
