using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Utility.MML
{
	public class MMLParser
	{
		char currentChar => restString.FirstOrDefault();
		string restString;

		public MMLParser(string arguments)
		{
			restString = arguments;
		}

		public IEnumerable<MMLObject> Parse()
		{
			List<MMLObject> mml = new List<MMLObject>();
			while (!string.IsNullOrWhiteSpace(restString))
			{
				if (Accept('-'))
				{
					string name = ParseName();
					if (Accept(':'))
					{
						mml.Add(new MMLObject(name, ParseValue()));
					}
					else
					{
						mml.Add(new MMLObject(name, true));
					}
				}
				else
				{
					Next();
				}
			}
			return mml;
		}

		public static T Serialize<T>(string arguments)
		{
			var mml = new MMLParser(arguments).Parse()
				.ToDictionary(x => x.Key, x => x.Value);

			T t = Activator.CreateInstance<T>();

			foreach (var i in t.GetType().GetProperties())
			{
				if (mml.ContainsKey(i.Name.ToLower()))
				{
					Type type = i.PropertyType;
					if(Nullable.GetUnderlyingType(type) != null)
					{
						type = Nullable.GetUnderlyingType(type);
					}
					MethodInfo method = type.GetMethod("Parse", new[] { typeof(string) }, null);
					object parsedOutput = method.Invoke(null, new[] { mml[i.Name.ToLower()].ToString() });

					i.SetValue(t, parsedOutput);
				}
			}
			return t;
		}

		public static object SerializeGeneric(string type, string arguments)
		{
			Type[] types = Assembly.GetEntryAssembly().GetTypes();
			var wantedType = types.Where(x => x.Name.ToLower() == type.ToLower());
			object genType = Activator.CreateInstance(wantedType.First());
			MMLParser mml = new MMLParser(arguments);
			MethodInfo method = mml.GetType().GetMethod("Serialize")
										 .MakeGenericMethod(new Type[] { genType.GetType() });
			return method.Invoke(mml, null);
		}

		public bool Accept(char c)
		{
			if (currentChar == c)
			{
				Next();
				return true;
			}
			return false;
		}

		private void Next()
		{
			restString = restString.Substring(1);
		}

		private string TakeUntil(char c)
		{
			string val = restString.Split(c)[0];
			restString = restString.Substring(val.Length);
			return val;
		}

		private string ParseName()
		{
			string output = "";
			while(restString[0] != ':' && restString[0] != ' ')
			{
				output += restString.First();
				Next();
			}
			return output;
		}

		private object ParseValue()
		{
			if(Accept('"'))
			{
				string value = TakeUntil('"');
				return value;
			}
			else
			{
				string value = TakeUntil(' ');
				if(int.TryParse(value, out int num))
				{
					return num;
				}
				else if(bool.TryParse(value, out bool b))
				{
					return b;
				}
				return value;
			}
		}
	}
}
	