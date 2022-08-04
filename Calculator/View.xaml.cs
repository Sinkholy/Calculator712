using System.Collections.Generic;
using System.Windows;

namespace Calculator712.Calculator
{
	/// <summary>
	/// Interaction logic for Window.xaml
	/// </summary>
	public partial class PanelsHolder : Window
	{
		readonly GridMesh mainMesh;

		internal PanelsHolder()
		{
			InitializeComponent();

			mainMesh = GridMesh.AssignTo(MainGrid);
		}

		internal void ApplyLayout(Layout layout)
		{
			mainMesh.Reset();
			mainMesh.SetSize(layout.RowsSize, layout.ColumnsSize);

			foreach (var panel in layout.Panels)
			{
				mainMesh.Pick(panel.Position.Row, panel.Position.Column).Content = panel.UIElement;
			}
		}

		internal class Layout
		{
			internal int RowsSize { get; init; }
			internal int ColumnsSize { get; init; }
			internal ICollection<Controller.IPanel> Panels { get; init; }
		}
	}
}