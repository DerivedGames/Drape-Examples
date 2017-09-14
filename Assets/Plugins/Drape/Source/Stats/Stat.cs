﻿using System.Collections.Generic;
using Drape.Interfaces;
using Drape.Slug;

namespace Drape
{
	public class Stat : BaseStat<StatData>, IStat
	{
		private List<Modifier> _modifiers = new List<Modifier>();
		private Dictionary<IStat, float> _dependencies = new Dictionary<IStat, float>();

		private Modifier _modTotals;

		private string ModTotalsName { get { return this.Name + " totals"; } }

		public Stat(StatData data, IRegistry registry) : base(data, registry)
		{
			// process for serialization
			if (data.dependencies != null) {
				foreach (StatData.Dependency dep in data.dependencies) {
					IStat stat = registry.Get<IStat>(dep.Code);
					_dependencies.Add(stat, dep.Value);
				}
			}

			ResetModifierTotals();
		}

		/// <summary>
		/// Stat value after applying modifiers and dependencies.
		/// </summary>
		public override float Value
		{
			get
			{
				float depsValue = 0;
				// todo: is there more efficient way?
				foreach (KeyValuePair<IStat, float> entry in _dependencies) {
					IStat stat = entry.Key;
					float factor = entry.Value;
					depsValue += stat.Value * factor;
				}
				float baseValue = base.BaseValue + depsValue;
				float value = _modTotals.GetValue(baseValue);
				return value;
			}
		}

		/// <summary>
		/// Returns number of modifiers
		/// </summary>
		public float ModifierCount
		{
			get
			{
				return _modifiers.Count;
			}
		}

		public StatData.Dependency[] Dependencies { get { return _data.dependencies; } }

		public void AddModifier(Modifier modifier)
		{
			if (modifier.Stat != this.Code) {
				string e = System.String.Format("Mod type mismatch. Modifier \"{0}\" for  stat: \"{1}\" is not allowed by stat: \"{2}\"", modifier.Name, modifier.Stat, this.Name);
				throw new System.Exception(e);
			}
			_modifiers.Add(modifier);
			AddModifierValues(modifier);
		}

		public void RemoveMod(Modifier mod)
		{
			_modifiers.Remove(mod);
			ResetModifierTotals();
		}

		public void ClearMods()
		{
			_modifiers.Clear();
			ResetModifierTotals();
		}

		private void ResetModifierTotals()
		{
			_modTotals = new Modifier(new ModifierData(ModTotalsName.ToSlug(), ModTotalsName, this.Name, 0, 1, 0, 1), _registry);

			if (_modifiers != null) {
				foreach (Modifier modifier in _modifiers) {
					AddModifierValues(modifier);
				}
			}
		}

		private void AddModifierValues(Modifier modifier)
		{
			int rawFlat = _modTotals.RawFlat + modifier.RawFlat;
			float rawFactor = _modTotals.RawFactor * (1 + modifier.RawFactor);
			int finalFlat = _modTotals.FinalFlat + modifier.FinalFlat;
			float finalFactor = _modTotals.FinalFactor * (1 + modifier.FinalFactor);

			_modTotals = new Modifier(new ModifierData(ModTotalsName.ToSlug(), ModTotalsName, this.Name, rawFlat, rawFactor, finalFlat, finalFactor), _registry);
		}


		/*
		// ModTotals is a copy of Modifier class but has public members
		private class ModTotals: BaseStat<ModifierData>, IStat
		{
			public float RawFlat { get; set; }
			public float RawFactor { get; set; }
			public float FinalFlat { get; set; }
			public float FinalFactor { get; set; }

			public ModTotals(ModifierData data, IRegistry registry) : base(data, registry) { }

			public float GetValue(float baseValue)
			{
				return ((baseValue + _data.RawFlat) * _data.RawFactor + _data.FinalFlat) * _data.FinalFactor;
			}
		}
		*/
	}
}