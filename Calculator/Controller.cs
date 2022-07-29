using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

using static Calculator712.Calculator.View;

namespace Calculator712.Calculator
{
	public class Controller
	{
		public View View { get; }
		public XDocument Layout { get; set; }
		ICalculatorOperation[] Operations { get; }

		public Action<XDocument> LayoutSaveRequested = delegate { };

		public Controller(ICalculatorOperation[] operations, XDocument layout)
		{
			Operations = operations;
			View = new View(layout, Operations.Select(x => x.Symbol).ToArray());
			View.CalculationRequested += OnCalculationRequested;
			View.LayoutSaveRequested += layout => LayoutSaveRequested(layout);
		}

		void OnCalculationRequested(CalculationData data)
		{
			var targetOperation = GetOperationBySymbol(data.OperationSymbol);
			var result = targetOperation.Calculate(data.LeftOperand, data.RightOperand);
			View.SetResult(result);
		}
		ICalculatorOperation GetOperationBySymbol(string symbol)
		{
			foreach(var operation in Operations)
			{
				if(operation.Symbol == symbol)
				{
					return operation;
				}
			}
			return null;
		}
	}
}
