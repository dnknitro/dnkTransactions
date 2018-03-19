using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using dnkTransactionsUI.Domain;
using dnkTransactionsUI.Service;
using dnkTransactionsUI.Utils;
using DevExpress.Data.Filtering;
using DevExpress.Xpf.Grid;

namespace dnkTransactionsUI
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public BindingList<Transaction> TransactionsSource { get; } = new BindingList<Transaction>();
		
		public MainWindow()
		{
			var formConfig = new FormUIconfigSupportWPF(this, false);
			// ReSharper disable once AssignNullToNotNullAttribute
			var layoutXml = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, nameof(TransactionsGrid) + "Layout.xml");
			formConfig.AddCustomParamProvider(
				nameof(TransactionsGrid) + "Layout",
				delegate
				{
					TransactionsGrid?.SaveLayoutToXml(layoutXml);
					return "";
				},
				delegate
				{
					if (File.Exists(layoutXml))
						TransactionsGrid?.RestoreLayoutFromXml(layoutXml);
				});
			formConfig.LoadAndApplyValues();

			LoadDataFromFile();
			new FileChangesWatcher(LoadDataFromFile);

			InitializeComponent();
		}

		private void LoadDataFromFile()
		{
			// ReSharper disable once AssignNullToNotNullAttribute
			var files = Directory.GetFiles(App.PathToDataFile.DirectoryName, App.PathToDataFile.Name.Replace(".csv", "*.csv"));
			var file = files.OrderByDescending(x => new FileInfo(x).LastWriteTime).FirstOrDefault();

			var transactions = new TransactionsService().LoadTransactions(file, App.LimitToYear).ToList();
			Title = $"dnk Transactions {App.LimitToYear} {file}";
			TransactionsGrid?.BeginDataUpdate();
			TransactionsSource.RaiseListChangedEvents = false;
			TransactionsSource.Clear();
			foreach (var transaction in transactions)
				TransactionsSource.Add(transaction);
			TransactionsSource.RaiseListChangedEvents = true;
			TransactionsGrid?.EndDataUpdate();
		}

		private void IncludeByValueCellMenuItem_Click( object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e )
		{
			var menuInfo = TransactionsGridTableView.GridMenu.MenuInfo as GridCellMenuInfo;
			if( menuInfo?.Row != null && menuInfo.Column != null )
			{
				var column = (GridColumn) menuInfo.Column;
				var value = TransactionsGrid.GetCellValue(menuInfo.Row.RowHandle.Value, column);
				TransactionsGrid.FilterCriteria &= new BinaryOperator(column.FieldName, value, BinaryOperatorType.Equal);
			}
		}

		private void ExcludeByValueCellMenuItem_Click( object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e )
		{
			var menuInfo = TransactionsGridTableView.GridMenu.MenuInfo as GridCellMenuInfo;
			if( menuInfo?.Row != null && menuInfo.Column != null )
			{
				var column = (GridColumn) menuInfo.Column;
				var value = TransactionsGrid.GetCellValue(menuInfo.Row.RowHandle.Value, column);
				TransactionsGrid.FilterCriteria &= new BinaryOperator(column.FieldName, value, BinaryOperatorType.NotEqual);
			}
		}
	}
}