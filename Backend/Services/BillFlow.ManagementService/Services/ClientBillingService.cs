using BillFlow.Models.Dtos.Auth.Account;
using BillFlow.Models.Dtos.Billing;
using BillFlow.Models.Entities;
using BillFlow.Repositories.Interfaces;
using BillFlow.ManagementService.Services.Billing;

namespace BillFlow.ManagementService.Services;

public sealed class ClientBillingService(
    IClientRepository clientRepository,
    ICurrentUserAccessor currentUser) : IClientBillingService
{
    public async Task<OperationResult<IReadOnlyList<ClientResponse>>> GetAllAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<IReadOnlyList<ClientResponse>>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var clients = await clientRepository.GetAllAsync(ownerId.Value!.Value, search, cancellationToken);
        return OperationResult<IReadOnlyList<ClientResponse>>.Ok(clients.Select(Map).ToList());
    }

    public async Task<OperationResult<ClientResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<ClientResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var client = await clientRepository.GetByIdAsync(ownerId.Value!.Value, id, cancellationToken);
        if (client is null)
            return NotFound<ClientResponse>();

        return OperationResult<ClientResponse>.Ok(Map(client));
    }

    public async Task<OperationResult<ClientResponse>> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<ClientResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        if (!TryValidateClientFields(request.CompanyName, request.ContactName, out var fieldError))
        {
            return OperationResult<ClientResponse>.Fail(
                fieldError!,
                StatusCodes.Status400BadRequest);
        }

        if (await clientRepository.EmailExistsForOwnerAsync(
                ownerId.Value!.Value,
                request.Email,
                cancellationToken: cancellationToken))
        {
            return OperationResult<ClientResponse>.Fail(
                "A client with this email already exists.",
                StatusCodes.Status409Conflict);
        }

        var client = new Client
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId.Value.Value,
            CompanyName = request.CompanyName.Trim(),
            ContactName = request.ContactName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Address = request.Address?.Trim(),
            Country = request.Country?.Trim(),
            TaxNumber = request.TaxNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true,
        };

        await clientRepository.CreateAsync(client, cancellationToken);
        return OperationResult<ClientResponse>.Ok(Map(client), StatusCodes.Status201Created);
    }

    public async Task<OperationResult<ClientResponse>> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<ClientResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        if (!TryValidateClientFields(request.CompanyName, request.ContactName, out var fieldError))
        {
            return OperationResult<ClientResponse>.Fail(
                fieldError!,
                StatusCodes.Status400BadRequest);
        }

        var client = await clientRepository.GetByIdAsync(ownerId.Value!.Value, id, cancellationToken);
        if (client is null)
            return NotFound<ClientResponse>();

        if (await clientRepository.EmailExistsForOwnerAsync(
                ownerId.Value.Value,
                request.Email,
                id,
                cancellationToken))
        {
            return OperationResult<ClientResponse>.Fail(
                "A client with this email already exists.",
                StatusCodes.Status409Conflict);
        }

        client.CompanyName = request.CompanyName.Trim();
        client.ContactName = request.ContactName.Trim();
        client.Email = request.Email;
        client.PhoneNumber = request.PhoneNumber?.Trim();
        client.Address = request.Address?.Trim();
        client.Country = request.Country?.Trim();
        client.TaxNumber = request.TaxNumber?.Trim();
        client.Notes = request.Notes?.Trim();
        client.IsActive = request.IsActive;

        await clientRepository.UpdateAsync(client, cancellationToken);
        return OperationResult<ClientResponse>.Ok(Map(client));
    }

    public async Task<OperationResult<MessageResponse>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var ownerId = BillingAuthorization.RequireBusinessOwnerId<MessageResponse>(currentUser);
        if (ownerId.Error is not null)
            return ownerId.Error;

        var client = await clientRepository.GetByIdAsync(ownerId.Value!.Value, id, cancellationToken);
        if (client is null)
            return NotFound<MessageResponse>();

        if (await clientRepository.HasInvoicesAsync(ownerId.Value.Value, id, cancellationToken))
        {
            return OperationResult<MessageResponse>.Fail(
                "Cannot delete a client that has invoices. Deactivate the client instead.",
                StatusCodes.Status400BadRequest);
        }

        await clientRepository.SoftDeleteAsync(ownerId.Value.Value, id, cancellationToken);
        return OperationResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Client deleted successfully.",
        });
    }

    private static bool TryValidateClientFields(
        string companyName,
        string contactName,
        out string? error)
    {
        if (!BillingInputValidator.TryValidateRequiredText(companyName, "Company name", out error))
            return false;

        return BillingInputValidator.TryValidateRequiredText(contactName, "Contact name", out error);
    }

    private static OperationResult<T> NotFound<T>() =>
        OperationResult<T>.Fail("Client not found.", StatusCodes.Status404NotFound);

    private static ClientResponse Map(Client client) => new()
    {
        Id = client.Id,
        CompanyName = client.CompanyName,
        ContactName = client.ContactName,
        Email = client.Email,
        PhoneNumber = client.PhoneNumber,
        Address = client.Address,
        Country = client.Country,
        TaxNumber = client.TaxNumber,
        Notes = client.Notes,
        IsActive = client.IsActive,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt,
    };
}
