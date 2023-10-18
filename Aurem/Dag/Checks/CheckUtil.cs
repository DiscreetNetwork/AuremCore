using Aurem.Model;
using Aurem.Model.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Dag.Checks
{
    /// <summary>
    /// CheckUtil provides the main <see cref="UnitChecker"/> implementations used for setup and consensus.
    /// </summary>
    public static class CheckUtil
    {
        /// <summary>
        /// BasicCorrectness implements <see cref="UnitChecker"/> and returns a version of the dag that will check the following notion of correctness:
        /// <list type="number">
        /// <item>The unit must have nProc parents such that the nth parent is created by the nth process.</item>
        /// <item>The unit has to have a predecessor or have all <see langword="null" /> parents.</item>
        /// <item>The unit is a prime unit.</item>
        /// </list>
        /// If any of these checks fail, a <see cref="ComplianceException"/> will be thrown.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="dag"></param>
        /// <exception cref="ComplianceException"></exception>
        public static void BasicCorrectness(IUnit u, IDag dag)
        {
            var parents = u.Parents().ToArray();
            var nProc = dag.NProc();

            if (parents.Length != nProc)
            {
                throw new ComplianceException("Wrong number of parents");
            }
            ushort nonNullParents = 0;

            for (ushort i = 0; i < nProc; i++)
            {
                if (parents[i] == null) continue;
                nonNullParents++;
                if (parents[i].Creator() != i)
                {
                    throw new ComplianceException("nth parent not created by nth process");
                }
            }

            if (u.Predecessor() == null && nonNullParents > 0)
            {
                throw new ComplianceException("unit without a predecessor but with other parents");
            }

            if (u.Predecessor() != null && u.Predecessor().Level() >= u.Level())
            {
                throw new ComplianceException("non-prime unit");
            }
        }

        /// <summary>
        /// NoLevelSkipping implements <see cref="UnitChecker"/> and ensures that no process creates a unit of level n > 0 without creating a unit of level n-1. Checks that it is sufficient to test whether height = level for every unit.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="_"></param>
        /// <exception cref="ComplianceException"></exception>
        public static void NoLevelSkipping(IUnit u, IDag _)
        {
            if (u.Level() != u.Height())
            {
                throw new ComplianceException("the level of the unit is different than its height");
            }
        }

        /// <summary>
        /// NoForks implements <see cref="UnitChecker"/> and ensures that forked units are not added to the DAG.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="dag"></param>
        /// <exception cref="ComplianceException"></exception>
        public static void NoForks(IUnit u, IDag dag)
        {
            var maxes = dag.MaximalUnitsPerProcess().Get(u.Creator());
            if (maxes.Count == 0)
            {
                return;
            }

            var max = maxes[0];
            if (max.Height() >= u.Height())
            {
                throw new ComplianceException("the unit is a fork");
            }
        }

        /// <summary>
        /// Checks whether the unit has sufficient evidence of the given creator forking, i.e. it is above two units created by the creator that share a predecessor.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        private static bool HasForkingEvidence(IUnit u, ushort creator)
        {
            if (u.Dealing()) return false;
            var f = u.Floor(creator).ToList();
            return f.Count > 1 || (f.Count == 1 && !f[0].PEquals(u.Parents().ToArray()[creator]));
        }

        /// <summary>
        /// ParentConsistency implements <see cref="UnitChecker"/> and checks the parent consistency rule; i.e., the units nth parent cannot be lower (by level) than the nth parent of any other of the unit's parent.
        /// Essentially, units seen from U "directly" (parents) cannot be below the ones seen "indirectly" (parents of parents).
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="dag"></param>
        /// <exception cref="ComplianceException"></exception>
        public static void ParentConsistency(IUnit unit, IDag dag)
        {
            var parents = unit.Parents().ToList();
            var nProc = dag.NProc();

            for (ushort i = 0; i < nProc; i++)
            {
                for (ushort j = 0; j < nProc; j++)
                {
                    if (parents[j] == null) continue;
                    var u = parents[j].Parents().ToList()[i];
                    if (u != null && (parents[i] == null || parents[i].Level() < u.Level()))
                    {
                        throw new ComplianceException("parent consistency rule violated");
                    }
                }
            }
        }

        /// <summary>
        /// NoSelfForkingEvidence implements <see cref="UnitChecker"/> and checks if a unit does not provide evidence of its creator forking.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="dag"></param>
        /// <exception cref="ComplianceException"></exception>
        public static void NoSelfForkingEvidence(IUnit u, IDag dag)
        {
            if (HasForkingEvidence(u, u.Creator()))
            {
                throw new ComplianceException("A unit has evidence of self forking");
            }
        }

        /// <summary>
        /// ForkerMuting implements <see cref="UnitChecker"/> and checks if a unit's parents respects the forker-muting policy, where the following situation is not allowed:
        /// <list type="bullet">
        /// <item> There exists a process j such that one of the parents was created by j, AND</item>
        /// <item> One of the parents has evidence that j is forking.</item>
        /// </list>
        /// </summary>
        /// <param name="u"></param>
        /// <param name="dag"></param>
        /// <exception cref="ComplianceException"></exception>
        public static void ForkerMuting(IUnit u, IDag dag)
        {
            foreach (var parent1 in u.Parents())
            {
                if (parent1 == null) continue;
                foreach (var parent2 in u.Parents())
                {
                    if (parent2 == null) continue;
                    if (parent1 == parent2) continue;

                    if (HasForkingEvidence(parent1, parent2.Creator()))
                    {
                        throw new ComplianceException("Some parent has evidence of another parent being a forker");
                    }
                }
            }
        }
    }
}
