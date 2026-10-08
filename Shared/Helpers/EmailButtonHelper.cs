namespace SaborExpress.Shared.Helpers
{
    public static class EmailButtonHelper
    {
        public static string Build(string text, string url) =>
            "<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin:24px 0;\"><tr>" +
            "<td style=\"background:#e4572e;border-radius:8px;\">" +
            $"<a href=\"{url}\" style=\"display:inline-block;padding:14px 28px;color:#ffffff;" +
            $"font-family:Arial,sans-serif;font-weight:bold;text-decoration:none;\">{text}</a>" +
            "</td></tr></table>" +
            $"<p style=\"font-size:12px;color:#888;\">Si el boton no funciona, copia este enlace en tu navegador:<br>{url}</p>";
    }
}