using Autofac;
using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Services;
using Pathfinder1eHelper.ViewModels;
using Pathfinder1eHelper.ViewModels.Pages;
using Pathfinder1eHelper.Views;
using Pathfinder1eHelper.Views.Pages;

namespace Pathfinder1eHelper.Infrastructure;

/// <summary>
/// Central Autofac composition root. Registered via <c>UseReactiveUIWithAutofac</c> in
/// <see cref="Program"/>; the resulting container also becomes the Splat/ReactiveUI locator,
/// so the name-convention <see cref="ViewLocator"/> resolves views from here as well.
/// </summary>
public sealed class AppModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Data layer
        builder.RegisterType<DbPathProvider>().As<IDbPathProvider>().SingleInstance();
        builder.Register(c => FreeSqlFactory.CreateReadOnly(c.Resolve<IDbPathProvider>().SpellsDbPath))
            .As<IFreeSql>()
            .SingleInstance(); // IFreeSql is thread-safe → one shared instance
        builder.RegisterType<SpellRepository>().As<ISpellRepository>().InstancePerDependency();
        builder.RegisterType<SpellService>().As<ISpellService>().InstancePerDependency();

        // Shell
        builder.RegisterType<MainWindowViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<MainWindow>().AsSelf();

        // Pages (page VMs resolved by the shell via Func<T> factories; views by the ViewLocator)
        builder.RegisterType<SpellsViewModel>().AsSelf().InstancePerDependency();
        builder.RegisterType<SpellsView>().AsSelf().InstancePerDependency();
    }
}