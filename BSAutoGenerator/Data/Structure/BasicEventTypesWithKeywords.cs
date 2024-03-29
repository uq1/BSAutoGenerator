﻿using System.Collections.Generic;
using System.Text.Json.Serialization;
using static BSAutoGenerator.Info.Helper;

namespace BSAutoGenerator.Data.Structure
{
    internal class BasicEventTypesWithKeywords
    {
		public class BasicEventTypesForKeyword
		{
			[JsonInclude]
			[JsonPropertyName("k")]
			public string keyword;

			[JsonInclude]
			[JsonPropertyName("e")]
			public int eventType;

			public BasicEventTypesForKeyword(string keyword, int eventType)
			{
				this.keyword = keyword;
				this.eventType = eventType;
			}
		}

		[JsonInclude]
		[JsonPropertyName("d")]
		public List<BasicEventTypesForKeyword> data = new();

		[JsonConstructor]
		public BasicEventTypesWithKeywords(List<BasicEventTypesForKeyword> data)
		{
			this.data = data;
		}
    }
}
