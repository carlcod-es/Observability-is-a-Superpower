using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

using SuperPower.Forms.Storage;

namespace SuperPower.Forms;

/// <summary>Registers package services. The site registers only its own <see cref="IFormSubmissionHandler"/>s.</summary>
public sealed class SuperPowerFormsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<FormValidator>();
        builder.Services.AddSingleton<FormDefinitionReader>();
        builder.Services.AddScoped<FormSubmissionHandlers>();
        builder.Services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
    }
}
