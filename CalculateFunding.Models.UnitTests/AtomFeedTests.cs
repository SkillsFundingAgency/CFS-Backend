using System.IO;
using System.Xml.Serialization;
using CalculateFunding.Models.External.AtomItems;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CalculateFunding.Models.UnitTests
{
	[TestClass]
	public class AtomFeedTests
	{
		[TestMethod]
		public void SerializeXml_WhenIsNotArchived_ShouldNotSerializeValue()
		{
			// arrange 
			AtomFeed<string> atomFeedDummyModel = new AtomFeed<string>()
			{
				Id = "Id",
				Title = "Title",
				IsArchived = false
			};

			XmlSerializer serializer = new XmlSerializer(typeof(AtomFeed<string>));
			StringWriter stringWriter = new StringWriter();

			// act
			serializer.Serialize(stringWriter, atomFeedDummyModel);

			// assert
			string generatedXml = stringWriter.ToString();

			generatedXml.Should().NotContain("IsArchived");
			generatedXml.Should().NotContain("Archive");
		}

		[TestMethod]
		public void SerializeJson_WhenIsNotArchived_ShouldNotSerializeValue()
		{
			// arrange 
			AtomFeed<string> atomFeedDummyModel = new AtomFeed<string>()
			{
				Id = "Id",
				Title = "Title",
				IsArchived = false
			};

			// act
			string generatedJson = JsonConvert.SerializeObject(atomFeedDummyModel);

			// assert
			generatedJson.Should().NotContain("IsArchived");
			generatedJson.Should().NotContain("Archive");
		}

		[TestMethod]
		public void SerializeXml_WhenIsArchived_ShouldSerializeValue()
		{
			// arrange 
			AtomFeed<string> atomFeedDummyModel = new AtomFeed<string>()
			{
				Id = "Id",
				Title = "Title",
				IsArchived = true
			};

			var serializer = new XmlSerializer(typeof(AtomFeed<string>));
			StringWriter stringWriter = new StringWriter();

			// act
			serializer.Serialize(stringWriter, atomFeedDummyModel);

			// assert
			string generatedXml = stringWriter.ToString();
			generatedXml.Should().NotContain("IsArchived");
			generatedXml.Should().Contain("Archive");
		}

		[TestMethod]
		public void SerializeJson_WhenIsArchived_ShouldSerializeValue()
		{
			// arrange 
			AtomFeed<string> atomFeedDummyModel = new AtomFeed<string>()
			{
				Id = "Id",
				Title = "Title",
				IsArchived = true
			};

			// act
			string generatedJson = JsonConvert.SerializeObject(atomFeedDummyModel);

			// assert

			generatedJson.Should().NotContain("IsArchived");
			generatedJson.Should().Contain("Archive");
		}
	}
}
