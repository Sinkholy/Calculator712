using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace Calculator712
{
	class GridMesh // TODO: как мне показалось здесь много потенциальных мест для появления ошибок
					// это касается методов-выборок и каунтеров т.к. во многих из них идёт обращение к
					// свойству Count первого элемента массива рядов, а он может оказаться null.
	{
		readonly Grid grid;
		readonly List<List<Cell>> rows;
		readonly CellsEnumerable cells;

		public static implicit operator Grid(GridMesh mesh)
		{
			return mesh.grid;
		}

		internal static GridMesh AssignTo(Grid grid)
		{
			return new GridMesh(grid);
		}
		internal static GridMesh CreateWithSize(int rows, int columns)
		{
			return new GridMesh(rows, columns);
		}
		internal GridMesh(Grid grid)
		{
			this.grid = grid;
			rows = new List<List<Cell>>();
			cells = new CellsEnumerable(rows);
			AdaptToExisingLayout();
		}
		internal GridMesh()
		{
			grid = new Grid();
			rows = new List<List<Cell>>();
			cells = new CellsEnumerable(rows);
		}
		internal GridMesh(int rows, int columns)
			: this()
		{
			SetSize(rows, columns);
		}

		internal bool IsEmpty => RowsCount == 0; // TODO: мне не нравится нейминг.
												// Это свойство говорит о том, что ячеек просто не существует
												// а не о том, что есть ячейки, но они пустые.
		internal int RowsCount => rows.Count;
		internal int ColumnsCount => IsEmpty
									 ? 0 
									 : rows[0].Count;
		internal int CellsCount => IsEmpty 
								   ? 0 
								   : rows.Count * rows[0].Count;
		internal CellsEnumerable Cells => cells;
		internal IEnumerable<IEnumerable<Cell>> Rows => rows;
		internal IEnumerable<IEnumerable<Cell>> Columns
			=> Rows.Select((_, i) => Rows.Select(row => row.ElementAt(i)));
		internal IEnumerable<Cell> GetRow(int index)
		{
			return index >= RowsCount
						? Enumerable.Empty<Cell>()
						: rows[index];
		}
		internal IEnumerable<Cell> GetColumn(int index)
			=> rows.Select(row => row.ElementAt(index));
		internal Cell? Pick(int row, int column)
		{
			return IsOutOfRange() 
				? null 
				: rows[row][column];

			bool IsOutOfRange()
			{
				return row >= RowsCount || column >= ColumnsCount;
			}
		}
		internal void AddRow()
		{
			AddRowToGrid();
			AddVirtualRow();
		}
		internal void AddColumn()
		{
			AddColumnToGrid();
			AddVirtualColumn();
		}
		void AddVirtualColumn()
		{
			var cells = CreateCells();
			for (int i = 0; i < RowsCount; i++)
			{
				var row = rows[i];
				var cell = cells[i];
				row.Add(cell);
			}

			List<Cell> CreateCells()
			{
				var result = new List<Cell>(RowsCount);
				for (int i = 0; i < RowsCount; i++)
				{
					result.Add(CreateCell(i, ColumnsCount));
				}
				return result;
			}
		}
		void AddVirtualRow()
		{
			var row = new List<Cell>(ColumnsCount);
			var cells = CreateCells();
			row.AddRange(cells);
			rows.Add(row);

			List<Cell> CreateCells()
			{
				var result = new List<Cell>(ColumnsCount);
				for (int i = 0; i < ColumnsCount; i++)
				{
					result.Add(CreateCell(rows.Count, i));
				}
				return result;
			}
		}
		Cell CreateCell(int row, int column)
		{
			var cell = new Cell(row, column);
			cell.ContentChanged += OnCellContentChanged;
			cell.ContentCleared += OnCellContentCleared;
			cell.VisibilityChanged += OnCellVisibilityChanged;
			return cell;
		}
		void AddRowToGrid()
		{
			grid.RowDefinitions.Add(new RowDefinition());
		}
		void AddColumnToGrid()
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition());
		}
		internal void SetSize(int rows, int columns) // TODO: возможна ли передача нулей?
		{
			for (int i = RowsCount; i < rows; i++)
			{
				AddRow();
			}
			for (int i = ColumnsCount; i < columns; i++)
			{
				AddColumn();
			}
		}
		void AdaptToExisingLayout()
		{
			var targetColumnsCount = grid.ColumnDefinitions.Count;
			var targetRowsCount = grid.RowDefinitions.Count;

			for (int i = RowsCount; i < targetRowsCount; i++)
			{
				AddVirtualRow();
			}
			for (int i = ColumnsCount; i < targetColumnsCount; i++)
			{
				AddVirtualColumn();
			}
		}
		void OnCellContentChanged(Cell.ContentChangedArgs args)
		{
			grid.Children.Remove(args.PreviousContent);
			grid.Children.Add(args.NewContent);
			AllignContentToGrid(args.NewContent, args.CellRow, args.CellColumn);
		}
		void OnCellContentCleared(Cell.ContentChangedArgs args)
		{
			grid.Children.Remove(args.PreviousContent);
		}
		void OnCellVisibilityChanged(Cell.VisibilityChangedArgs args)
		{
			var cell = Pick(args.CellRow, args.CellColumn);
			cell.Content.Visibility = args.New;
		}
		internal void SwapCells(Cell a, Cell b)
		{
			int aOriginalRow = a.Row;
			int aOriginalColumn = a.Column;
			ChangeCellPosition(a, b.Row, b.Column);
			ChangeCellPosition(b, aOriginalRow, aOriginalColumn);
		}
		internal void ChangeCellPosition(Cell cell, int targetRow, int targetColumn) // TODO: проверка параметров
		{
			cell.Row = targetRow;
			cell.Column = targetColumn;
			AllignContentToGrid(cell.Content, targetRow, targetColumn);
			RemoveCellFromOriginalPosition();
			AddCellToNewLocation();

			void RemoveCellFromOriginalPosition()
			{
				rows[cell.Row][cell.Column] = null;
			}
			void AddCellToNewLocation()
			{
				rows[targetRow][targetColumn] = cell;
			}
		}
		void AllignContentToGrid(UIElement content, int row, int column)
		{
			Grid.SetRow(content, row);
			Grid.SetColumn(content, column);
		}

		internal class Cell
		{
			internal Action<ContentChangedArgs> ContentChanged = delegate { };
			internal Action<ContentChangedArgs> ContentCleared = delegate { };
			internal Action<VisibilityChangedArgs> VisibilityChanged = delegate { };

			internal Cell(int row, int column)
			{
				Row = row;
				Column = column;
			}

			internal bool Filled => Content != null;
			internal bool Blocked { get; set; }
			public int Column { get; internal set; }
			public int Row { get; internal set; }
			UIElement _content;
			internal UIElement Content
			{
				get => _content;
				set
				{
					RaiseEvent();
					_content = value;

					void RaiseEvent()
					{
						var eventArgs = new ContentChangedArgs
						{
							CellColumn = Column,
							CellRow = Row,
							PreviousContent = Content,
							NewContent = value
						};
						if (value is null)
						{
							ContentCleared(eventArgs);
						}
						else
						{
							ContentChanged(eventArgs);
						}
					}
				}
			}
			Visibility _visibility;
			internal Visibility Visibility
			{
				get => _visibility;
				set
				{
					var eventArgs = new VisibilityChangedArgs
					{
						CellRow = Row,
						CellColumn = Column,
						Previous = _visibility,
						New = value
					};
					VisibilityChanged(eventArgs);
					_visibility = value;
				}
			}

			internal void ClearContent()
			{
				Content = null;
			}

			internal class VisibilityChangedArgs
			{
				internal int CellRow { get; init; }
				internal int CellColumn { get; init; }
				internal Visibility Previous { get; init; }
				internal Visibility New { get; init; }
			}
			internal class ContentChangedArgs
			{
				internal int CellRow { get; init; }
				internal int CellColumn { get; init; }
				internal UIElement PreviousContent { get; init; }
				internal UIElement NewContent { get; init; }
			}
		}
		internal class CellsEnumerable : IEnumerable<Cell>
		{
			readonly List<List<Cell>> rows;

			public CellsEnumerable(List<List<Cell>> rows)
			{
				this.rows = rows;
			}

			internal int Count => rows.Count == 0 ? 0 : rows.Count * rows[0].Count;
			internal bool ContainsEmptyCells => Empty.Any();
			internal int EmptyCount => Empty.Count();
			internal IEnumerable<Cell> Empty => this.Where(cell => !cell.Filled);
			internal IEnumerable<Cell> Filled => this.Where(cell => cell.Filled);

			public IEnumerator<Cell> GetEnumerator()
			{
				return new CellEnumerator(this);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			class CellEnumerator : IEnumerator<Cell>
			{
				readonly CellsEnumerable cells;
				int currentRow;
				int currentColumn;

				public CellEnumerator(CellsEnumerable cells)
				{
					this.cells = cells;
					currentRow = 0;
					currentColumn = -1;
				}

				public Cell Current => cells.rows[currentRow][currentColumn];
				object IEnumerator.Current => Current;
				int RowsCount => cells.rows.Count;
				int ColumnsCount => cells.rows.Count == 0 ? 0 : cells.rows[0].Count;

				public void Dispose() { }
				public bool MoveNext()
				{
					if (currentColumn++ == ColumnsCount - 1)
					{
						currentRow++;
						currentColumn = 0;
					}
					return currentRow != RowsCount;
				}
				public void Reset()
				{
					currentRow = 0;
					currentColumn = -1;
				}
			}
		}
	}
}