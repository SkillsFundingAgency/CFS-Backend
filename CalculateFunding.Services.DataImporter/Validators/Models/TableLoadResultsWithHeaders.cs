﻿using CalculateFunding.Models.Datasets;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class TableLoadResultWithHeaders
    {
	    public TableLoadResult TableLoadResult { get; set; }

	    public IDictionary<string, int> RetrievedHeaderFields { get; set; }
    }
}
