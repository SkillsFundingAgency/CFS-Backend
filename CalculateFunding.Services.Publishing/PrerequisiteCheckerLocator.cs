using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Publishing
{
    public class PrerequisiteCheckerLocator : IPrerequisiteCheckerLocator
    {
        private readonly IEnumerable<IPrerequisiteChecker> _checkers;

        public PrerequisiteCheckerLocator(IEnumerable<IPrerequisiteChecker> checkers)
        {
            _checkers = checkers;
        }

        public IPrerequisiteChecker GetPreReqChecker(PrerequisiteCheckerType type)
        {
            return _checkers.SingleOrDefault(_ => _.IsCheckerType(type)) ?? throw new ArgumentOutOfRangeException();
        }
    }
}
