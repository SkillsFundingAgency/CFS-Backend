using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Comparers
{
    public class PublishedProviderVersionComparer : IEqualityComparer<PublishedProviderVersion>
    {
        public IEnumerable<string> Variances => _variances;

        private readonly List<string> _variances;

        public PublishedProviderVersionComparer()
        {
            _variances = new List<string>();
        }

        private (bool, T) CompareEnumerable<T>(IEnumerable<T> firstEnumerable, IEnumerable<T> secondEnumerable, Func<T, T, bool> predicate)
        {
            T mismatchedObject = default(T);

            if (!firstEnumerable.IsNullOrEmpty())
            {
                if (!secondEnumerable.IsNullOrEmpty())
                {
                    if (firstEnumerable.Count() != secondEnumerable.Count() || !firstEnumerable.All(x =>
                    {
                        if (secondEnumerable.Any(y => predicate(x, y)))
                        {
                            return true;
                        }

                        mismatchedObject = x;
                        return false;
                    }))
                    {
                        return (false, mismatchedObject);
                    }
                }
                else
                {
                    return (false, mismatchedObject);
                }
            }
            else
            {
                if (!secondEnumerable.IsNullOrEmpty())
                {
                    return (false, mismatchedObject);
                }
            }

            return (true, mismatchedObject);
        }

        public bool Equals(PublishedProviderVersion x, PublishedProviderVersion y)
        {
            (bool equal, FundingLine fundingLine) hasFundingLineChanges = CompareEnumerable(x.FundingLines, y.FundingLines, (xfl, yfl) => {
                if (xfl.TemplateLineId == yfl.TemplateLineId && xfl.FundingLineCode == yfl.FundingLineCode && xfl.Name == yfl.Name && xfl.Type == yfl.Type && xfl.Value == yfl.Value)
                {
                    (bool equal, DistributionPeriod distributionPeriod) hasDistributionPeriodChanges = CompareEnumerable(xfl.DistributionPeriods, yfl.DistributionPeriods, (xdp, ydp) => {
                        {
                            if (xdp.DistributionPeriodId == ydp.DistributionPeriodId && xdp.Value == ydp.Value)
                            {
                                (bool equal, ProfilePeriod profilePeriod) hasProfilePeriodChanges = CompareEnumerable(xdp.ProfilePeriods, ydp.ProfilePeriods, (xpp, ypp) => xpp.DistributionPeriodId == ypp.DistributionPeriodId && xpp.ProfiledValue == ypp.ProfiledValue && xpp.TypeValue == ypp.TypeValue && xpp.Type == ypp.Type && xpp.Year == ypp.Year && xpp.Occurrence == ypp.Occurrence);
                                if (!hasProfilePeriodChanges.equal)
                                {
                                    _variances.Add($"ProfilePeriod:{hasProfilePeriodChanges.profilePeriod?.DistributionPeriodId}");
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }

                            return true;
                        }
                    });

                    if (!hasDistributionPeriodChanges.equal)
                    {
                        _variances.Add($"DistributionPeriod:{hasDistributionPeriodChanges.distributionPeriod?.DistributionPeriodId}");
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                return true;
            });

            if (!hasFundingLineChanges.equal)
            {
                _variances.Add($"FundingLine:{hasFundingLineChanges.fundingLine?.FundingLineCode}");
                return false;
            }

            (bool equal, FundingCalculation calculation) hasCalcChanges = CompareEnumerable(x.Calculations, y.Calculations, (xc, yc) => xc.TemplateCalculationId == yc.TemplateCalculationId && xc.Value.ToString() == yc.Value.ToString());

            if (!hasCalcChanges.equal)
            {
                _variances.Add($"Calculation:{hasCalcChanges.calculation?.TemplateCalculationId}");
                return false;
            }

            (bool equal, FundingReferenceData reference) hasReferenceChanges = CompareEnumerable(x.ReferenceData, y.ReferenceData, (xr, yr) => xr.TemplateReferenceId == yr.TemplateReferenceId && xr.Value.Equals(yr.Value));

            if (!hasReferenceChanges.equal)
            {
                _variances.Add($"ReferenceData:{hasReferenceChanges.reference?.TemplateReferenceId}");
                return false;
            }

            if (x.TemplateVersion != y.TemplateVersion)
            {
                _variances.Add($"TemplateVersion: {x.TemplateVersion} != {y.TemplateVersion}");
                return false;
            }

            ProviderComparer providerComparer = new ProviderComparer();

            if (!providerComparer.Equals(x.Provider, y.Provider))
            {
                providerComparer.Variances.ToList().ForEach(_ =>
                {
                    _variances.Add($"Provider: {_.Key}: {_.Value}");
                });

                return false;
            }

            return true;
        }

        public int GetHashCode(PublishedProviderVersion obj)
        {
            throw new NotImplementedException();
        }
    }
}
