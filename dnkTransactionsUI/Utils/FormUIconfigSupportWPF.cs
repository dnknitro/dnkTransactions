using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using Application = System.Windows.Application;

namespace dnkTransactionsUI.Utils
{
	public class CustomParamProvider
	{
		public string Key { get; set; }
		public Func<string> Getter { get; set; }
		public Action<string> Setter { get; set; }
	}

	//can't be unit tested because constructor requires Form object
	public class FormUIconfigSupportWPF
	{
		protected Window _form;

		private const string ATTR_ID = "id";
		private const string ATTR_VALUE = "value";
		private const string ATTR_UPDATED = "updated";
		private const string FILENAME = "FormUIconfigSupport.xml";

		private class IdAndValue
		{
			public string Id { get; }

			public string Val { get; }

			public DateTime Updated { get; }

			public IdAndValue(string id, string val, DateTime updated)
			{
				Id = id;
				Val = val;
				Updated = updated;
			}
		}

		protected static readonly object _padlock = new object();
		private static Dictionary<string, IdAndValue> _geometryToValuesList;

		private readonly List<CustomParamProvider> CustomParamProviders = new List<CustomParamProvider>();

		protected Rectangle _beforeMaxMin;
		protected string _formID;

		private static Dictionary<string, IdAndValue> GeometryList
		{
			get
			{
				lock (_padlock)
				{
					return _geometryToValuesList ?? (_geometryToValuesList = LoadValuesList());
				}
			}
		}

		public void AddCustomParamProvider(string key, Func<string> getter, Action<string> setter)
		{
			CustomParamProviders.Add(new CustomParamProvider
			{
				Key = key,
				Getter = getter,
				Setter = setter
			});
		}

		/// <summary>
		///     For unit tests only
		/// </summary>
		protected static void ClearGeometryList()
		{
			lock (_padlock)
			{
				_geometryToValuesList = null;
			}
		}

		public void LoadAndApplyValues()
		{
			if (GeometryList.ContainsKey(_formID))
			{
				var lastLoadedValues = DeserializeValues(GeometryList[_formID].Val);

				foreach (var provider in CustomParamProviders)
				{
					if (!lastLoadedValues.ContainsKey(provider.Key)) continue;
					provider.Setter(lastLoadedValues[provider.Key]);
				}
			}
		}

		protected void UpdateListWithCurrentValues()
		{
			var serializedValue = SerializeCurrentValues();

			var g = new IdAndValue(_formID, serializedValue, DateTime.Now);
			lock (_padlock)
			{
				GeometryList[g.Id] = g;
			}
		}

		private Dictionary<string, string> GetCurrentValues()
		{
			var currentValues = new Dictionary<string, string>();

			foreach (var pair in CustomParamProviders)
				currentValues[pair.Key] = pair.Getter();

			return currentValues;
		}

		private string SerializeCurrentValues()
		{
			var currentValues = (from KeyValuePair<string, string> pair in GetCurrentValues()
				select $"{pair.Key}={HttpUtility.UrlEncode(pair.Value)}").ToList();

			return string.Join("&", currentValues);
		}

		/// <summary>
		///     deserializes parameters and sets windows size, location, state from saved values
		/// </summary>
		/// <param name="serializedValue"></param>
		private Dictionary<string, string> DeserializeValues(string serializedValue)
		{
			var loadedValues = new Dictionary<string, string>();

			if (!string.IsNullOrEmpty(serializedValue))
			{
				var nameValueCollection = HttpUtility.ParseQueryString(serializedValue);
				foreach (string key in nameValueCollection.Keys)
					if (!string.IsNullOrEmpty(key))
						loadedValues[key] = nameValueCollection[key];
			}

			return loadedValues;
		}

		protected void SizeOrLocationChanged(Window form)
		{
			_beforeMaxMin.Width = (int) form.Width;
			_beforeMaxMin.Height = (int) form.Height;
			_beforeMaxMin.X = (int) form.Left;
			_beforeMaxMin.Y = (int) form.Top;
		}

		/// <summary>
		///     loads geometries from file. If file doesn't exist - returns empty list of geometries, doesn't create the file
		/// </summary>
		/// <returns></returns>
		private static Dictionary<string, IdAndValue> LoadValuesList()
		{
			var valuesList = new Dictionary<string, IdAndValue>();

			var xml = IsolatedStorageHelper.Load(FILENAME);

			if (string.IsNullOrEmpty(xml)) return valuesList;

			var doc = new XmlDocument();
			try
			{
				doc.LoadXml(xml);
			}
			catch (XmlException)
			{
				//Log.Warn(ex.Message, ex);
				IsolatedStorageHelper.Delete(FILENAME);
				return valuesList;
			}

			if (doc.DocumentElement == null)
				return valuesList;

			// ReSharper disable PossibleNullReferenceException
			foreach (XmlNode node in doc.DocumentElement.GetElementsByTagName("formGeometry"))
			{
				if (node.Attributes[ATTR_ID] == null || node.Attributes[ATTR_VALUE] == null || node.Attributes[ATTR_UPDATED] == null)
					//skip erroneous nodes
					continue;

				var g = new IdAndValue(node.Attributes[ATTR_ID].Value, node.Attributes[ATTR_VALUE].Value,
					DateTime.Parse(node.Attributes[ATTR_UPDATED].Value));
				valuesList.Add(g.Id, g);
			}
			// ReSharper restore PossibleNullReferenceException

			return valuesList;
		}

		/// <summary>
		///     Saves the windows configuration to disk.
		/// </summary>
		public static void SaveGeometryList()
		{
			lock (_padlock)
			{
				//create xml and write it
				var doc = new XmlDocument();
				doc.LoadXml("<formGeometries></formGeometries>");

				if (doc.DocumentElement != null)
				{
					// ReSharper disable PossibleNullReferenceException
					foreach (var pair in GeometryList)
					{
						XmlNode node = doc.CreateElement("formGeometry");
						node.Attributes.Append(doc.CreateAttribute(ATTR_ID));
						node.Attributes.Append(doc.CreateAttribute(ATTR_VALUE));
						node.Attributes.Append(doc.CreateAttribute(ATTR_UPDATED));
						node.Attributes[ATTR_ID].Value = pair.Value.Id;
						node.Attributes[ATTR_VALUE].Value = pair.Value.Val;
						node.Attributes[ATTR_UPDATED].Value = pair.Value.Updated.ToString();
						doc.DocumentElement.AppendChild(node);
					}
					// ReSharper restore PossibleNullReferenceException

					IsolatedStorageHelper.Save(FILENAME, doc.OuterXml);
				}
			}
		}

		public static string ScreensGeometryToString()
		{
			var screensGeometry = "";
			foreach (var s in Screen.AllScreens)
				screensGeometry += s.WorkingArea;
			return screensGeometry;
		}

		public const string HORIZONTAL_SPLITTER_DISTANCE_KEY = "HorizontalSplitterDistance";

		private static bool _applicationExitSet;
		public static FormUIconfigSupportWPF LastCreatedInstance { get; private set; }

		public FormUIconfigSupportWPF(Window form) : this(form, false)
		{
		}

		public FormUIconfigSupportWPF(Window form, bool noSize)
		{
			LastCreatedInstance = this;

			//Form's location, size and WindowState of the form is stored on per _formID basis
			//_formID is generated from form's type name and ScreensGeometry configuration
			//Thus it is unique per different screen resolutions and multy monitors configuration
			_formID = form.GetType().Name + Regex.Replace(ScreensGeometryToString(), "[{},=]", string.Empty);

			lock (_padlock)
			{
				if (!_applicationExitSet)
				{
					_applicationExitSet = true;
					Application.Current.Exit += delegate { SaveGeometryList(); };
				}
			}

			//_systemWindow = new SystemWindow(form.Handle);
			_form = form;

			AddCustomParamProvider("FormX", () => _beforeMaxMin.X.ToString(), x => SetDouble(x, z => _form.Left = z, 0));
			AddCustomParamProvider("FormY", () => _beforeMaxMin.Y.ToString(), y => SetDouble(y, z => _form.Top = z, 0));
			if (!noSize)
			{
				AddCustomParamProvider("FormWidth", () => _beforeMaxMin.Width.ToString(), width => SetInt(width, z => _form.Width = z));
				AddCustomParamProvider("FormHeight", () => _beforeMaxMin.Height.ToString(), height => SetInt(height, z => _form.Height = z));
			}
			AddCustomParamProvider("FormWindowState", () => _form.WindowState.ToString(), windowString => { _form.WindowState = windowString == "Maximized" ? WindowState.Maximized : WindowState.Normal; });


			form.Initialized += delegate
			{
				LoadAndApplyValues();
				LastCreatedInstance = null;
				_form.Closing += delegate { UpdateListWithCurrentValues(); };
			};

			form.SizeChanged += Form_ResizeOrLocationChanged;
			form.LocationChanged += Form_ResizeOrLocationChanged;
		}

		private static double? GetDouble(string strVal)
		{
			if (!string.IsNullOrWhiteSpace(strVal) && double.TryParse(strVal, out double val))
				return val;
			return null;
		}

		private static void SetDouble(string strDouble, Action<double> setter, double? minValue = null)
		{
			var val = GetDouble(strDouble);
			if (val != null)
				setter(Math.Max(minValue ?? double.MinValue, val.Value));
		}

		private static int? GetInt(string strVal)
		{
			if (!string.IsNullOrWhiteSpace(strVal) && int.TryParse(strVal, out int val))
				return val;
			return null;
		}

		private static void SetInt(string strDouble, Action<int> setter)
		{
			var val = GetInt(strDouble);
			if (val != null)
				setter(val.Value);
		}

		private void Form_ResizeOrLocationChanged(object sender, EventArgs e)
		{
			if (!(_form.WindowState == WindowState.Maximized || _form.WindowState == WindowState.Minimized))
				SizeOrLocationChanged(_form);
		}
	}
}