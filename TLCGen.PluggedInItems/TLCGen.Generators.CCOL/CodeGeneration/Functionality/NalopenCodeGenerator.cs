﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLCGen.Generators.CCOL.CodeGeneration.HelperClasses;
using TLCGen.Generators.CCOL.Settings;
using TLCGen.Integrity;
using TLCGen.Models;
using TLCGen.Models.Enumerations;

namespace TLCGen.Generators.CCOL.CodeGeneration.Functionality
{
    [CCOLCodePieceGenerator]
    public class NalopenCodeGenerator : CCOLCodePieceGeneratorBase
    {
        #region Fields

#pragma warning disable 0649
        private CCOLGeneratorCodeStringSettingModel _hnla;
        private CCOLGeneratorCodeStringSettingModel _tnlfg;
        private CCOLGeneratorCodeStringSettingModel _tnlfgd;
        private CCOLGeneratorCodeStringSettingModel _tnlsg;
        private CCOLGeneratorCodeStringSettingModel _tnlsgd;
        private CCOLGeneratorCodeStringSettingModel _tnlcv;
        private CCOLGeneratorCodeStringSettingModel _tnlcvd;
        private CCOLGeneratorCodeStringSettingModel _tnleg;
        private CCOLGeneratorCodeStringSettingModel _tnlegd;
        private CCOLGeneratorCodeStringSettingModel _prmxnl;
#pragma warning restore 0649
	    private string _homschtegenh;

        #endregion // Fields

        public override void CollectCCOLElements(ControllerModel c)
        {
            _myElements = new List<CCOLElement>();

            foreach (var nl in c.InterSignaalGroep.Nalopen)
            {
                foreach (var nlt in nl.Tijden)
                {
                    var _tnl = nlt.Type switch
                    {
                        NaloopTijdTypeEnum.StartGroen => _tnlsg,
                        NaloopTijdTypeEnum.StartGroenDetectie => _tnlsgd,
                        NaloopTijdTypeEnum.VastGroen => _tnlfg,
                        NaloopTijdTypeEnum.VastGroenDetectie => _tnlfgd,
                        NaloopTijdTypeEnum.EindeGroen => _tnleg,
                        NaloopTijdTypeEnum.EindeGroenDetectie => _tnlegd,
                        NaloopTijdTypeEnum.EindeVerlengGroen => _tnlcv,
                        NaloopTijdTypeEnum.EindeVerlengGroenDetectie => _tnlcvd,
                        _ => null
                    };
                    _myElements.Add(
                        CCOLGeneratorSettingsProvider.Default.CreateElement(
                            $"{_tnl}{nl.FaseVan}{nl.FaseNaar}",
                            nlt.Waarde,
                            CCOLElementTimeTypeEnum.TE_type, 
                            _tnl, nl.FaseVan, nl.FaseNaar));
                }
                if (nl.DetectieAfhankelijk)
                {
                    foreach (var nld in nl.Detectoren)
                    {
                        var elem = CCOLGeneratorSettingsProvider.Default.CreateElement($"{_hnla}{nld.Detector}", _hnla, nld.Detector, nl.FaseVan, nl.FaseNaar);
                        if (_myElements.Count == 0 || _myElements.All(x => x.Naam != elem.Naam))
                        {
                            _myElements.Add(elem);
                        }
                    }
                }
                if(nl.MaximaleVoorstart.HasValue)
                {
                    _myElements.Add(
                        CCOLGeneratorSettingsProvider.Default.CreateElement(
                            $"{_prmxnl}{nl.FaseVan}{nl.FaseNaar}",
                            nl.MaximaleVoorstart.Value,
                            CCOLElementTimeTypeEnum.TE_type, 
                            _prmxnl, nl.FaseVan, nl.FaseNaar));
                }
            }
        }

        public override bool HasCCOLElements() => true;

        public override IEnumerable<CCOLLocalVariable> GetFunctionLocalVariables(ControllerModel c, CCOLCodeTypeEnum type)
        {
            switch (type)
            {
                case CCOLCodeTypeEnum.RegCMaxgroen:
                case CCOLCodeTypeEnum.RegCVerlenggroen:
                    if(c.InterSignaalGroep?.Nalopen?.Count > 0)
                        return new List<CCOLLocalVariable> { new("int", "fc") };
                    return base.GetFunctionLocalVariables(c, type);
                default:
                    return base.GetFunctionLocalVariables(c, type);
            }
        }

        public override int[] HasCode(CCOLCodeTypeEnum type)
        {
            return type switch
            {
                CCOLCodeTypeEnum.RegCInitApplication => new []{30},
                CCOLCodeTypeEnum.RegCPreApplication => new []{30},
                CCOLCodeTypeEnum.RegCSynchronisaties => new []{20},
                CCOLCodeTypeEnum.RegCMaxgroenNaAdd => new []{10},
                CCOLCodeTypeEnum.RegCVerlenggroenNaAdd => new []{10},
                CCOLCodeTypeEnum.RegCMaxgroen => new []{20},
                CCOLCodeTypeEnum.RegCVerlenggroen => new []{10},
                CCOLCodeTypeEnum.RegCAlternatieven => new []{20},
                CCOLCodeTypeEnum.PrioCPrioriteitsNiveau => new []{20},
                _ => null
            };
        }

        public override string GetCode(ControllerModel c, CCOLCodeTypeEnum type, string ts, int order)
        {
            var sb = new StringBuilder();

            switch (type)
            {
				case CCOLCodeTypeEnum.RegCInitApplication:
                    if (c.InterSignaalGroep?.Nalopen?.Count > 0)
                    {
                        sb.AppendLine($"{ts}/* Nalopen */");
                        sb.AppendLine($"{ts}/* ------- */");
                        sb.AppendLine($"{ts}gk_InitGK();");
                        sb.AppendLine($"{ts}gk_InitNL();");
                    }
                    return sb.ToString();
                case CCOLCodeTypeEnum.RegCPreApplication:
                    if (c.InterSignaalGroep?.Nalopen?.Count > 0)
                    {
                        sb.AppendLine($"{ts}/* Nalopen */");
                        sb.AppendLine($"{ts}/* ------- */");
                        sb.AppendLine($"{ts}gk_ResetGK();");
                        sb.AppendLine($"{ts}gk_ResetNL();");
                    }
                    // TODO: should only generate if any nalopen are there?
                    if (c.HalfstarData.IsHalfstar && _myElements.Any(x => x.Type == CCOLElementTypeEnum.Timer))
					{
                        sb.AppendLine();
						sb.AppendLine($"{ts}IH[{_hpf}{_homschtegenh}] |=");
						var k = 0;
						foreach (var t in _myElements.Where(x => x.Type == CCOLElementTypeEnum.Timer))
						{
							if (k != 0)
							{
								sb.AppendLine(" ||");
							}
							sb.Append($"{ts}{ts}T[{_tpf}{t.Naam}]");
							++k;
						}
						sb.AppendLine(";");
					}
					return sb.ToString();
                case CCOLCodeTypeEnum.RegCSynchronisaties:
                    if (c.Data.SynchronisatiesType != SynchronisatiesTypeEnum.RealFunc &&
                        c.InterSignaalGroep?.Nalopen?.Count > 0)
                    {
                        if (c.InterSignaalGroep.Nalopen.Any(x => x.MaximaleVoorstart.HasValue))
                        {
                            var nls = c.InterSignaalGroep.Nalopen.Where(x => x.MaximaleVoorstart.HasValue);
                            sb.AppendLine($"{ts}/* Tegenhouden voedende richtingen tot tijd t voor naloop mag komen */");
                            sb.AppendLine($"{ts}/* afzetten X */");
                            foreach (var nl in nls)
                            {
                                sb.AppendLine($"{ts}X[{_fcpf}{nl.FaseVan}] &= ~{_BITxnl};");
                            }
                            sb.AppendLine();
                            sb.AppendLine($"{ts}/* Tegenhouden voedende richtingen tot in 1 keer kan worden overgestoken */");
                            sb.AppendLine($"{ts}/* Betekenis {_prmpf}x##: tijd dat fase ## eerder mag komen dan SG nalooprichting */");
                            foreach (var nl in nls)
                            {
                                sb.AppendLine($"{ts}X[{_fcpf}{nl.FaseVan}] |= x_aanvoer({_fcpf}{nl.FaseNaar}, PRM[{_prmpf}{_prmxnl}{nl.FaseVan}{nl.FaseNaar}]) ? {_BITxnl} : 0;");
                            }
                        }
                    }
                    return sb.ToString();

                case CCOLCodeTypeEnum.RegCMaxgroen:
                case CCOLCodeTypeEnum.RegCVerlenggroen:
                    if (c.InterSignaalGroep?.Nalopen?.Count > 0)
                    {
                        sb.AppendLine($"{ts}/* Nalopen */");
                        sb.AppendLine($"{ts}/* ------- */");
                        sb.AppendLine();
                        sb.AppendLine($"{ts}for (fc = 0; fc < FCMAX; ++fc)");
                        sb.AppendLine($"{ts}{{");
                        sb.AppendLine($"{ts}{ts}RW[fc] &= ~BIT2;");
                        sb.AppendLine($"{ts}{ts}YV[fc] &= ~BIT2;");
                        sb.AppendLine($"{ts}{ts}YM[fc] &= ~BIT2;");
                        sb.AppendLine($"{ts}}}");
                        sb.AppendLine();
                        foreach (var nl in c.InterSignaalGroep.Nalopen)
                        {
                            var vn = nl.FaseVan + nl.FaseNaar;
                            switch (nl.Type)
                            {
#warning This only works for pedestrians
                                case NaloopTypeEnum.StartGroen:
                                    if(nl.VasteNaloop)
                                    {
                                        sb.AppendLine($"{ts}NaloopVtg({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlsg}{vn});");
                                    }
                                    if (nl.DetectieAfhankelijk && nl.Detectoren?.Count > 0)
                                    {
                                        sb.AppendLine($"{ts}NaloopVtgDet({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_dpf}{nl.Detectoren[0].Detector}, {_hpf}{_hnla}{nl.Detectoren[0].Detector}, {_tpf}{_tnlsgd}{vn});");
                                    }
                                    break;

                                case NaloopTypeEnum.EindeGroen:
                                    if(nl.VasteNaloop)
                                    {
                                        sb.AppendLine($"{ts}NaloopFG({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlfg}{vn});");
                                        sb.AppendLine($"{ts}NaloopEG({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnleg}{vn});");
                                    }
                                    if (nl.DetectieAfhankelijk && nl.Detectoren?.Count > 0)
                                    {
                                        sb.Append($"{ts}NaloopFGDet({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlfgd}{vn}");
                                        foreach (var d in nl.Detectoren)
                                        {
                                            sb.Append($", {_dpf}{d.Detector}");
                                        }
                                        sb.AppendLine(", END);");
                                        sb.Append($"{ts}NaloopEGDet({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlegd}{vn}");
                                        foreach (var d in nl.Detectoren)
                                        {
                                            sb.Append($", {_dpf}{d.Detector}");
                                        }
                                        sb.AppendLine(", END);");
                                    }
                                    break;

                                case NaloopTypeEnum.CyclischVerlengGroen:
                                    if (nl.VasteNaloop)
                                    {
										sb.AppendLine($"{ts}NaloopFG({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlfg}{vn});");
                                        sb.AppendLine($"{ts}NaloopCV({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlcv}{vn});");
                                    }
                                    if (nl.DetectieAfhankelijk && nl.Detectoren?.Count > 0)
                                    {
                                        sb.Append($"{ts}NaloopFGDet({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlfgd}{vn}");
                                        foreach (var d in nl.Detectoren)
                                        {
                                            sb.Append($", {_dpf}{d.Detector}");
                                        }
                                        sb.AppendLine(", END);");
                                        sb.Append($"{ts}NaloopCVDet({_fcpf}{nl.FaseVan}, {_fcpf}{nl.FaseNaar}, {_tpf}{_tnlcvd}{vn}");
                                        foreach (var d in nl.Detectoren)
                                        {
                                            sb.Append($", {_dpf}{d.Detector}");
                                        }
                                        sb.AppendLine(", END);");
                                    }
                                    break;
                            }
                        }
                        sb.AppendLine();
                    }
                    return sb.ToString();
                
                case CCOLCodeTypeEnum.RegCMaxgroenNaAdd:
                    if (c.InterSignaalGroep.Nalopen.Count > 0)
                        sb.AppendLine($"{ts}gk_ControlGK();");
                    return sb.ToString();
                
                case CCOLCodeTypeEnum.RegCVerlenggroenNaAdd:
                    if (c.InterSignaalGroep.Nalopen.Count > 0)
                        sb.AppendLine($"{ts}gk_ControlGK();");
                    return sb.ToString();
                
                case CCOLCodeTypeEnum.RegCAlternatieven:
                    if (c.InterSignaalGroep.Nalopen.Count > 0)
                    {
                        sb.AppendLine($"{ts}/* set meerealisatie voor richtingen met nalopen */");
                        sb.AppendLine($"{ts}/* --------------------------------------------- */");
                        foreach (var nl in c.InterSignaalGroep.Nalopen)
                        {
                            var sgv = c.Fasen.FirstOrDefault(x => x.Naam == nl.FaseVan);
                            var sgn = c.Fasen.FirstOrDefault(x => x.Naam == nl.FaseNaar);
                            if (nl.DetectieAfhankelijk && nl.Detectoren?.Count > 0 && 
                                sgv is { Type: FaseTypeEnum.Voetganger } && sgn is { Type: FaseTypeEnum.Voetganger })
                            {
                                sb.Append($"{ts}set_MRLW({_fcpf}{nl.FaseNaar}, {_fcpf}{nl.FaseVan}, ({c.GetBoolV()}) (SG[{_fcpf}{nl.FaseVan}] && A[{_fcpf}{nl.FaseNaar}] && (");
                                var i = 0;
                                foreach (var d in nl.Detectoren)
                                {
                                    if (i > 0) sb.Append(" || ");
                                    ++i;
                                    sb.Append($"IH[{_hpf}{_hnla}{d.Detector}]");
                                }
                                sb.AppendLine($") && !kcv({_fcpf}{nl.FaseNaar})));");
                            }
                            else
                            {
                                sb.AppendLine($"{ts}set_MRLW_nl({_fcpf}{nl.FaseNaar}, {_fcpf}{nl.FaseVan}, ({c.GetBoolV()}) (G[{_fcpf}{nl.FaseVan}] && !G[{_fcpf}{nl.FaseNaar}] && A[{_fcpf}{nl.FaseNaar}]));");
                            }
                        }
                    }
                    return sb.ToString();
                
                case CCOLCodeTypeEnum.PrioCPrioriteitsNiveau:
                    //if(!c.InterSignaalGroep.Nalopen.Any()) return "";
                    //sb.AppendLine($"{ts}/* Tegenhouden OV prio met conflict met nalooprichting indien die nog moet komen */");
                    //foreach (var nl in c.InterSignaalGroep.Nalopen)
                    //{
                    //    foreach (var ov in c.OVData.OVIngrepen.Where(x => TLCGenControllerChecker.IsFasenConflicting(c, nl.FaseNaar, x.FaseCyclus)))
                    //    {
                    //        sb.AppendLine($"{ts}iXPrio[ovFC{ov.FaseCyclus}] |= G[{_fcpf}{nl.FaseVan}] && CV[{_fcpf}{nl.FaseVan}] && !G[{_fcpf}{nl.FaseNaar}] &&;");
                    //    }
                    //}
                    //sb.AppendLine();
                    return sb.ToString();

                default:
                    return null;
            }
        }

        public override bool SetSettings(CCOLGeneratorClassWithSettingsModel settings) 
        { 
            _homschtegenh = CCOLGeneratorSettingsProvider.Default.GetElementName("homschtegenh");
            return base.SetSettings(settings);
	    }
    }
}