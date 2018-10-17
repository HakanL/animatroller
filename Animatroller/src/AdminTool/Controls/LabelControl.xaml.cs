using System.Windows;
using System.Windows.Controls;

namespace Animatroller.AdminTool.Controls
{
    /// <summary>
    /// Interaction logic for LabelControl.xaml
    /// </summary>
    public partial class LabelControl : UserControl
    {
        public LabelControl()
        {
            InitializeComponent();
        }

        /// <summary>Gets/Sets Title</summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>Dependency property to Get/Set the current Title</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(LabelControl), new PropertyMetadata("(Title)"));
    }
}
