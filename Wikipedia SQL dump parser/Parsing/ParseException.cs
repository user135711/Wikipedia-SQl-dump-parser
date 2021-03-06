﻿using System;

namespace WpSqlDumpParser.Parsing
{
	class ParseException : ApplicationException
	{
		static readonly string message = "Parsing error in function {0}.";

		public ParseException(string functionName)
			: base(string.Format(message, functionName))
		{ }
	}
}
