using System.Windows.Forms;

namespace VOTACAO.UI.Navigation;

public interface INavigationService
{
    void SetContainer(Panel container);
    void NavigateTo<TView>() where TView : UserControl;
}
