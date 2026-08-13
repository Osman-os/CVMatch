using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CVMatch.Web.Models.Validation;

/// <summary>
/// Onay kutusunun işaretlenmiş olmasını zorunlu kılar.
/// jQuery tarafında "required" kuralı olarak çalışır — checkbox için doğru davranış.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class OnayGerekliAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value) => value is true;

    public void AddValidation(ClientModelValidationContext context)
    {
        var message = FormatErrorMessage(context.ModelMetadata.GetDisplayName());

        context.Attributes.TryAdd("data-val", "true");
        context.Attributes.TryAdd("data-val-required", message);
    }
}