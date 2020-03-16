using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPrerequisiteCheckerLocator
    {
        IPrerequisiteChecker GetPreReqChecker(PrerequisiteCheckerType type);
    }
}
