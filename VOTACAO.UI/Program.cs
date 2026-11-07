using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VOTACAO.BO.Services;
using VOTACAO.Data.Migrations;
using VOTACAO.Data.Repositories;
using VOTACAO.Domain.Interfaces;
using VOTACAO.Infrastructure.Configuration;
using VOTACAO.Infrastructure.Database;
using VOTACAO.Infrastructure.Logging;
using VOTACAO.UI.Forms;
using VOTACAO.UI.Navigation;
using VOTACAO.UI.Views;

namespace VOTACAO.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            MessageBox.Show($"Ocorreu um erro crítico não tratado:\n{(e.ExceptionObject as Exception)?.Message}",
                "Erro Fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configBuilder.Build();

        var appSettings = new AppSettings();
        configuration.Bind(appSettings);

        var services = new ServiceCollection();

        services.AddSingleton(appSettings);
        services.AddSingleton<IAppLogger, FileLogger>();
        services.AddSingleton<MdbConnectionFactory>();

        services.AddSingleton<DbMigrationEngine>();
        services.AddScoped<IParticipanteRepository, ParticipanteRepository>();
        services.AddScoped<ICandidatoOpcaoRepository, CandidatoOpcaoRepository>();
        services.AddScoped<IEleicaoRepository, EleicaoRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IJuradoRepository, JuradoRepository>();
        services.AddScoped<ICriterioRepository, CriterioRepository>();
        services.AddScoped<IAvaliacaoNotaRepository, AvaliacaoNotaRepository>();

        services.AddScoped<IParticipanteService, ParticipanteService>();
        services.AddScoped<IJuradoService, JuradoService>();
        services.AddScoped<ICriterioService, CriterioService>();
        services.AddScoped<IApuracaoNotasService, ApuracaoNotasService>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<SobreView>();
        services.AddTransient<CadastradosView>();
        services.AddTransient<JuradosView>();
        services.AddTransient<CriteriosView>();
        services.AddTransient<ApuracaoNotasView>();
        services.AddTransient<ResultadosSqlView>();
        services.AddTransient<DiculgacaoView>();
        services.AddSingleton<Principal>();

        var provider = services.BuildServiceProvider();

        try
        {
            var migrationEngine = provider.GetRequiredService<DbMigrationEngine>();
            migrationEngine.InitializeAndMigrate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao inicializar o banco de dados Microsoft Access (.MDB):\n{ex.Message}",
                "Erro de Banco de Dados", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var mainForm = provider.GetRequiredService<Principal>();
        Application.Run(mainForm);
    }
}
