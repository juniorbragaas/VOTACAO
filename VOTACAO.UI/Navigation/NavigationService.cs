using Microsoft.Extensions.DependencyInjection;

namespace VOTACAO.UI.Navigation;

public class NavigationService : INavigationService
{
    private Panel? _container;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, UserControl> _viewCache = new();

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetContainer(Panel container)
    {
        _container = container;
    }

    public void NavigateTo<TView>() where TView : UserControl
    {
        if (_container == null)
            throw new InvalidOperationException("O container pnlConteudo ainda não foi configurado no NavigationService.");

        Type viewType = typeof(TView);
        if (!_viewCache.TryGetValue(viewType, out var viewInstance))
        {
            viewInstance = _serviceProvider.GetRequiredService<TView>();
            viewInstance.Dock = DockStyle.Fill;
            _viewCache[viewType] = viewInstance;
        }

        _container.SuspendLayout();
        _container.Controls.Clear();
        _container.Controls.Add(viewInstance);
        _container.ResumeLayout(true);

        if (viewInstance is IRefreshableView refreshable)
        {
            refreshable.OnNavigatedTo();
        }
    }
}

public interface IRefreshableView
{
    void OnNavigatedTo();
}
