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

using static Calculator712.Calculator.View;

namespace Calculator712.Calculator
{
	/// <summary>
	/// Interaction logic for Window.xaml
	/// </summary>
	public partial class View : Window
	{
		const string CalculationButtonSymbol = "=";
		const string BackspaceButtonSymbol = "<-";

		NumericButtonsPanel numericPanel;

		public Action<CalculationData> CalculationRequested = delegate { };

		public View()
		{
			InitializeComponent();
			var numericPanelLayout = new DefaultNumpadLayout();
			numericPanel = new NumericButtonsPanel(numericPanelLayout);
			numericPanel.ButtonPressed += NumericButtonClickHandler;

			ButtonsGrid.Children.Add(numericPanel);
			Grid.SetColumn(numericPanel, 0);
		}

		public void AddOperation(ICalculatorOperation operation)
		{
			var operationButton = new Button()
			{
				Content = operation.Symbol
			};
			operationButton.Click += OperationButtonClickHandler;
			ButtonsGrid.Children.Add(operationButton);
			Grid.SetColumn(operationButton, 1);
		}

		void CalculationButtonClickHandler(object sender, RoutedEventArgs args)
		{
			var expression = ResultTextBox.Text;
			string[] parsed = expression.Split(" ");
			var leftOperand = parsed[0];
			var operationSymbol = parsed[1];
			var rightOperand = parsed[2];
			var calculationData = new CalculationData(operationSymbol, leftOperand, rightOperand);
			CalculationRequested(calculationData);
		}
		void BackspaceButtonClickHandler(object sender, RoutedEventArgs args)
		{
			ResultTextBox.Text = "";
		}
		void NumericButtonClickHandler(int value)
		{
			AddStringToResultPanel(value.ToString());
		}
		void OperationButtonClickHandler(object sender, RoutedEventArgs args)
		{
			var button = sender as Button;
			string operation = button.Content as string;
			AddStringToResultPanel(" ");
			AddStringToResultPanel(operation);
			AddStringToResultPanel(" ");
		}
		void AddStringToResultPanel(string value)
		{
			ResultTextBox.Text += value;
		}
		public void SetResult(string value)
		{
			AddStringToResultPanel(" ");
			AddStringToResultPanel("=");
			AddStringToResultPanel(" ");
			AddStringToResultPanel(value);
		}

		public class CalculationData
		{
			public CalculationData(string operationSymbol, string leftOperand, string rightOperand)
			{
				OperationSymbol = operationSymbol;
				LeftOperand = leftOperand;
				RightOperand = rightOperand;
			}

			public string OperationSymbol { get; }
			public string LeftOperand { get; }
			public string RightOperand { get; }
		}

		internal abstract class MeshLayout
		{
			public abstract void ApplyTo(GridMesh target);
		}
		internal abstract class NumericPanelLayout : MeshLayout
		{
			public abstract void ApplyTo(GridMesh target, IButtonsProvider buttonsProvider);

			public interface IButtonsProvider
			{
				UIElement GetButtonWithNumber(int num);
			}
		}
		class DefaultNumpadLayout : NumericPanelLayout
		{
			const int ButtonsCount = 10;

			public override void ApplyTo(GridMesh target, IButtonsProvider buttonsProvider)
			{
				UIElement[] buttons = CreateButtons(buttonsProvider);
				target.Slice(4, 3);
				ApplyLayout(target, buttons);
			}
			UIElement[] CreateButtons(IButtonsProvider buttonsProvider)
			{
				UIElement[] result = new UIElement[ButtonsCount];
				for (int i = 0; i < ButtonsCount; i++)
				{
					result[i] = buttonsProvider.GetButtonWithNumber(i);
				}
				return result;
			}
			void ApplyLayout(GridMesh target, UIElement[] buttons)
			{
				target.Pick(3, 1).Content = buttons[0];
				target.Pick(0, 0).Content = buttons[1];
				target.Pick(0, 1).Content = buttons[2];
				target.Pick(0, 2).Content = buttons[3];
				target.Pick(1, 0).Content = buttons[4];
				target.Pick(1, 1).Content = buttons[5];
				target.Pick(1, 2).Content = buttons[6];
				target.Pick(2, 0).Content = buttons[7];
				target.Pick(2, 1).Content = buttons[8];
				target.Pick(2, 2).Content = buttons[9];
			}

			public override void ApplyTo(GridMesh target)
			{
				throw new NotImplementedException();
			}
		}
		class NumericButtonsPanel : Grid
		{
			GridMesh mesh;

			internal Action<int> ButtonPressed = delegate { };

			internal NumericButtonsPanel(NumericPanelLayout layout)
			{
				mesh = GridMesh.AssignTo(this);
				var provider = new NumericButtonProvider(ButtonClickHandler);
				layout.ApplyTo(mesh, provider);
			}

			void ButtonClickHandler(object sender, RoutedEventArgs args)
			{
				var button = sender as NumericButton;
				ButtonPressed(button.Value);
			}

			class NumericButtonProvider : NumericPanelLayout.IButtonsProvider
			{
				Action<object, RoutedEventArgs> callback;

				public NumericButtonProvider(Action<object, RoutedEventArgs> buttonClickCallback)
				{
					callback = buttonClickCallback;
				}

				public UIElement GetButtonWithNumber(int num)
				{
					var button = NumericButton.WithNumber(num);
					button.Click += (o, e) => callback(o, e);
					return button;
				}
			}
			class NumericButton : Button
			{
				internal static NumericButton WithNumber(int num)
				{
					return new NumericButton()
					{
						Value = num
					};
				}

				int value;
				internal int Value
				{
					get => value;
					set
					{
						this.value = value;
						Content = value;
					}
				}
			}
		}
	}
}