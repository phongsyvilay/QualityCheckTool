using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VioAlarmQualityCheckUtility.Class
{
	class IPValidator
	{
		public bool validateipv4(string ipstring)
		{
			byte tempforparsing;
			string[] splitvalues = ipstring.Split('.');

			if (string.IsNullOrWhiteSpace(ipstring))
				return false;

			if (splitvalues.Length > 4)
				return false;

			if (splitvalues.Length < 4)
				return false;

			foreach (string section in splitvalues)
			{
				if (!splitvalues.All(r => byte.TryParse(r, out tempforparsing)))
					return false;
			}

			return true;
		}
	}
}
