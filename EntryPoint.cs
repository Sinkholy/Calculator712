using Calculator712.Calculator;

using System;
using System.IO;
using System.Windows;
using System.Xml.Linq;

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
			var layout = LayoutHelper.GetLayoutXml();
			var calculator = new Controller(defaultOperations, layout);
			calculator.LayoutSaveRequested += LayoutHelper.SaveLayout;
			Run(calculator.View);
		}
		ICalculatorOperation[] CreateDefaultOperations()
		{
			var add = new Addition();
			var sub = new Substract();
			return new ICalculatorOperation[] { add, sub };
		}

		static class LayoutHelper
		{
			const string DefaultLayoutsFileName = "defaultLayout.xml";
			const string CustomLayoutsFileName = "layouts.xml";

			static string DefaultLayoutsFilePath => Directory.GetCurrentDirectory() + @"\" + DefaultLayoutsFileName;
			static string CustomLayoutsFilePath => Directory.GetCurrentDirectory() + @"\" + CustomLayoutsFileName;

			static internal XDocument GetLayoutXml()
			{
				XDocument layoutDoc;
				using (FileStream fs = OpenLayoutFile())
				{
					layoutDoc = XDocument.Load(fs); // TODO: try catch?
				}
				return layoutDoc;

				static FileStream OpenLayoutFile()
				{
					FileStream fs = File.Exists(CustomLayoutsFilePath)
								  ? OpenCustomLayoutFile()
								  : OpenDefaultLayoutFile();
					return fs;
				}
			}
			static FileStream OpenCustomLayoutFile()
			{
				return File.Open(CustomLayoutsFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			}
			static FileStream OpenDefaultLayoutFile()
			{
				if (!File.Exists(DefaultLayoutsFilePath))
				{
					throw new Exception(); // TODO: исключение
				}
				return File.Open(DefaultLayoutsFilePath, FileMode.Open, FileAccess.Read);
			}
			static internal void SaveLayout(XDocument layout)
			{
				// TODO: пусто
			}
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
