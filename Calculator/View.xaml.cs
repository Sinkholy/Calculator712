using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

using static Calculator712.Calculator.View;

namespace Calculator712.Calculator
{
	/// <summary>
	/// Interaction logic for Window.xaml
	/// </summary>
	public partial class View : Window
	{
		readonly GridMesh mainMesh;

		internal View()
		{
			InitializeComponent();

			mainMesh = GridMesh.AssignTo(MainGrid);
		}

		internal void ApplyLayout(PanelholderLayout layout)
		{
			mainMesh.Reset();
			mainMesh.SetSize(layout.RowsSize, layout.ColumnsSize);

			foreach (var panel in layout.Panels)
			{
				mainMesh.Pick(panel.Position.Row, panel.Position.Column).Content = panel.UIElement;
			}
		}

		internal class PanelholderLayout
		{
			internal int RowsSize { get; init; }
			internal int ColumnsSize { get; init; }
			internal ICollection<Controller.IPanel> Panels { get; init; }
		}
	}
}