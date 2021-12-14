using Calculator712.Calculator;

using System;
using System.Windows;

namespace Calculator712
{
	public interface ICalculatorOperation
	{
		string Description { get; }
		string Symbol { get; }
		string Name { get; }

		int Calculate(int leftOperand, int rightOperand);
	}
	public partial class EntryPoint : Application
	{
		public EntryPoint()
		{
		}

		[STAThread]
		public static void Main()
		{
			EntryPoint app = new();
			app.Inicialize();
		}
		void Inicialize()
		{
			var defaultOperations = CreateDefaultOperations();
			var calculator = new Controller(defaultOperations);
			calculator.Start();
			Run(calculator.View);
		}
		ICalculatorOperation[] CreateDefaultOperations()
		{
			var add = new Addition();
			var sub = new Substract();
			return new ICalculatorOperation[] { add, sub };
		}

		#region default operations
		public class Addition : ICalculatorOperation
		{
			public string Description { get; } = "Прибавляет правый операнд к левому.";
			public string Symbol { get; } = "+";
			public string Name { get; } = "Сумма";

			public int Calculate(int leftOperand, int rightOperand)
			{
				return leftOperand + rightOperand;
			}
		}
		public class Substract : ICalculatorOperation
		{
			public string Description { get; } = "Вычитает правый операнд из левого.";
			public string Symbol { get; } = "-";
			public string Name { get; } = "Разность";

			public int Calculate(int leftOperand, int rightOperand)
			{
				return leftOperand - rightOperand;
			}
		}
		#endregion
	}
}
