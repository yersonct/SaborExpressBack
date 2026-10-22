using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SaborExpress.Controllers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Interfaces;
using SaborExpress.Modules.Invoices.Models;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;
using Xunit;

namespace SaborExpressBack.InvoicePdf.Tests;

public sealed class InvoicePdfControllerTests
{
    [Fact]
    public async Task GetPdf_ReturnsDownloadablePrivatePdfForAuthorizedUser()
    {
        var fixture = new Fixture(branchId: 5, role: "GERENTE");
        var controller = fixture.CreateController();

        var result = await controller.GetPdf(41, "en");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("factura-41.pdf", file.FileDownloadName);
        Assert.Equal("private, no-store", fixture.HttpContext.Response.Headers.CacheControl);
        Assert.Equal("nosniff", fixture.HttpContext.Response.Headers.XContentTypeOptions);
        fixture.PdfGenerator.Verify(generator => generator.Generate(
            It.Is<InvoiceResponseDto>(invoice => invoice.Id == 41), "en"), Times.Once);
    }

    [Fact]
    public async Task GetPdf_UsesAcceptLanguageWhenQueryLanguageIsMissing()
    {
        var fixture = new Fixture(branchId: 5, role: "GERENTE");

        await fixture.CreateController().GetPdf(41);

        fixture.PdfGenerator.Verify(generator => generator.Generate(
            It.Is<InvoiceResponseDto>(invoice => invoice.Id == 41), "fr-CA,fr;q=0.9"), Times.Once);
    }

    [Fact]
    public async Task GetPdf_ReturnsNotFoundWhenInvoiceDoesNotExist()
    {
        var fixture = new Fixture(branchId: 5, role: "GERENTE", invoiceExists: false);
        var result = await fixture.CreateController().GetPdf(404);

        Assert.IsType<NotFoundResult>(result);
        fixture.PdfGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPdf_DeniesAdministratorOutsideOwnBranch()
    {
        var fixture = new Fixture(branchId: 5, role: "ADMINISTRADOR", invoiceBranchId: 6);
        var result = await fixture.CreateController().GetPdf(41);

        Assert.IsType<ForbidResult>(result);
        fixture.PdfGenerator.VerifyNoOtherCalls();
    }

    private sealed class Fixture
    {
        public Mock<IInvoicePdfGenerator> PdfGenerator { get; } = new();
        public DefaultHttpContext HttpContext { get; } = new();
        private readonly Mock<IInvoiceService> _invoiceService = new();
        private readonly Mock<IInvoiceRepository> _invoiceRepository = new();
        private readonly Mock<SaborExpress.Modules.Payments.Interfaces.IPaymentRepository> _paymentRepository = new();
        private readonly Mock<IOrderRepository> _orderRepository = new();
        private readonly Mock<IAuthRepository> _authRepository = new();

        public Fixture(int branchId, string role, bool invoiceExists = true, int invoiceBranchId = 5)
        {
            if (invoiceExists)
            {
                _invoiceRepository.Setup(repository => repository.GetByIdAsync(41))
                    .ReturnsAsync(new Invoice { Id = 41, OrderId = 72, BranchId = invoiceBranchId });
                _orderRepository.Setup(repository => repository.GetByIdAsync(72))
                    .ReturnsAsync(new Order { Id = 72, BranchId = invoiceBranchId });
                _invoiceService.Setup(service => service.GetByIdAsync(41))
                    .ReturnsAsync(new InvoiceResponseDto { Id = 41, InvoiceNumber = "QA-41" });
            }

            _authRepository.Setup(repository => repository.GetByIdWithRelationsAsync(12))
                .ReturnsAsync(new User { Id = 12, Employee = new Employee { BranchId = branchId } });

            PdfGenerator.Setup(generator => generator.Generate(It.IsAny<InvoiceResponseDto>(), It.IsAny<string?>()))
                .Returns("%PDF-test"u8.ToArray());

            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "12"),
                new Claim(ClaimTypes.Role, role)
            ], "pdf-test"));
            HttpContext.Request.Headers.AcceptLanguage = "fr-CA,fr;q=0.9";
        }

        public InvoicesController CreateController()
        {
            var controller = new InvoicesController(
                _invoiceService.Object,
                _invoiceRepository.Object,
                PdfGenerator.Object,
                _paymentRepository.Object,
                _orderRepository.Object,
                _authRepository.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
            return controller;
        }
    }
}
