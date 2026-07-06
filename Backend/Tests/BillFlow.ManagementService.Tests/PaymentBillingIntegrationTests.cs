using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Shared.Enums;
using Xunit;

namespace BillFlow.ManagementService.Tests;

[Collection("ManagementApi")]
public sealed class PaymentBillingIntegrationTests(ManagementApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Payments_UpdateInvoiceStatus_FromSentToPaid()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var invoice = await CreateSentInvoiceAsync(client);

        var partialResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoice.Id,
                Amount = 100m,
                Method = PaymentMethod.BankTransfer,
                Reference = "TXN-001",
            });
        Assert.Equal(HttpStatusCode.Created, partialResponse.StatusCode);

        var invoiceAfterPartial = await client.GetAsync($"/api/v1.0/billing/invoices/{invoice.Id}");
        var partialInvoice = await invoiceAfterPartial.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(partialInvoice);
        Assert.Equal(InvoiceStatus.PartiallyPaid, partialInvoice.Status);

        var finalResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoice.Id,
                Amount = 175m,
                Method = PaymentMethod.Cash,
            });
        Assert.Equal(HttpStatusCode.Created, finalResponse.StatusCode);

        var invoiceAfterPaid = await client.GetAsync($"/api/v1.0/billing/invoices/{invoice.Id}");
        var paidInvoice = await invoiceAfterPaid.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(paidInvoice);
        Assert.Equal(InvoiceStatus.Paid, paidInvoice.Status);

        var paymentsResponse = await client.GetAsync($"/api/v1.0/billing/invoices/{invoice.Id}/payments");
        var payments = await paymentsResponse.Content.ReadFromJsonAsync<List<PaymentResponse>>(JsonOptions);
        Assert.NotNull(payments);
        Assert.Equal(2, payments.Count);
    }

    [Fact]
    public async Task CreatePayment_ReturnsBadRequest_WhenOverpaying()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var invoice = await CreateSentInvoiceAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoice.Id,
                Amount = invoice.Total + 1m,
                Method = PaymentMethod.Cash,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefundPayment_RevertsInvoiceToSent()
    {
        var token = await RegisterAndLoginVisitorAsync();
        var client = CreateManagementClient(token);
        var invoice = await CreateSentInvoiceAsync(client);

        var paymentResponse = await client.PostAsJsonAsync(
            "/api/v1.0/billing/payments",
            new CreatePaymentRequest
            {
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Method = PaymentMethod.Stripe,
            });
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);

        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);
        Assert.NotNull(payment);

        var refundResponse = await client.PostAsync($"/api/v1.0/billing/payments/{payment.Id}/refund", null);
        Assert.Equal(HttpStatusCode.OK, refundResponse.StatusCode);

        var invoiceResponse = await client.GetAsync($"/api/v1.0/billing/invoices/{invoice.Id}");
        var updatedInvoice = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatus.Sent, updatedInvoice.Status);
    }

    private async Task<InvoiceDetailResponse> CreateSentInvoiceAsync(HttpClient client)
    {
        var billingClient = await client.PostAsJsonAsync(
            "/api/v1.0/billing/clients",
            new CreateClientRequest
            {
                CompanyName = "Payment Client",
                ContactName = "Payee",
                Email = $"pay-client-{Guid.NewGuid():N}@billflow.test",
            });
        var createdClient = await billingClient.Content.ReadFromJsonAsync<ClientResponse>(JsonOptions);
        Assert.NotNull(createdClient);

        var createInvoice = await client.PostAsJsonAsync(
            "/api/v1.0/billing/invoices",
            new CreateInvoiceRequest
            {
                ClientId = createdClient.Id,
                TaxRate = 10m,
                LineItems =
                [
                    new InvoiceLineItemRequest
                    {
                        Description = "Service",
                        Quantity = 1,
                        UnitPrice = 250m,
                    },
                ],
            });
        var invoice = await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(invoice);

        await client.PostAsync($"/api/v1.0/billing/invoices/{invoice.Id}/send", null);

        var sentResponse = await client.GetAsync($"/api/v1.0/billing/invoices/{invoice.Id}");
        var sentInvoice = await sentResponse.Content.ReadFromJsonAsync<InvoiceDetailResponse>(JsonOptions);
        Assert.NotNull(sentInvoice);
        Assert.Equal(InvoiceStatus.Sent, sentInvoice.Status);
        return sentInvoice;
    }

    private async Task<string> RegisterAndLoginVisitorAsync()
    {
        var authClient = fixture.AuthFactory.CreateClient();
        var email = $"visitor-{Guid.NewGuid():N}@billflow.test";
        const string password = "SecurePass123!";

        await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/register",
            new RegisterRequest
            {
                FullName = "Payment Visitor",
                Email = email,
                Password = password,
                ConfirmPassword = password,
            });

        var loginResponse = await authClient.PostAsJsonAsync(
            "/api/v1.0/auth/account/login",
            new LoginRequest { Email = email, Password = password });

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.AccessToken);
        return auth.AccessToken;
    }

    private HttpClient CreateManagementClient(string accessToken)
    {
        var httpClient = fixture.ManagementFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return httpClient;
    }
}
