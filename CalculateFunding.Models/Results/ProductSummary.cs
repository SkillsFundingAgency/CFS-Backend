﻿using System.Collections.Generic;
using CalculateFunding.Models.Scenarios;
using Newtonsoft.Json;
using Calculation = CalculateFunding.Models.Specs.Calculation;

namespace CalculateFunding.Models.Results
{
    public class ProductSummary : ResultSummary
    {
        public ProductSummary(string id, string name, Calculation calculation, string description, List<TestScenario> testScenarios)
        {
            Name = name;
            this.Calculation = calculation;
            Description = description;
            this.TestScenarios = testScenarios;
        }

        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("calculation")]
        public Calculation Calculation { get; set; }
        [JsonProperty("testScenarios")]
        public List<TestScenario> TestScenarios { get; set; }
        [JsonProperty("testProviders")]
        public List<Reference> TestProviders { get; set; }
    }
}