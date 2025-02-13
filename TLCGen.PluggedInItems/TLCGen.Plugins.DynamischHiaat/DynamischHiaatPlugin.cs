﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using TLCGen.Plugins.DynamischHiaat.Models;
using TLCGen.Plugins.DynamischHiaat.ViewModels;
using TLCGen.Plugins.DynamischHiaat.Views;
using TLCGen.Extensions;
using TLCGen.Generators.CCOL.CodeGeneration;
using TLCGen.Generators.CCOL.Settings;
using TLCGen.Helpers;
using TLCGen.Models;
using TLCGen.Dependencies.Providers;
using System;
using System.IO;
using TLCGen.Generators.CCOL.CodeGeneration.HelperClasses;
using TLCGen.Models.Enumerations;

namespace TLCGen.Plugins.DynamischHiaat
{
    [TLCGenTabItem(-1, TabItemTypeEnum.DetectieTab)]
    [TLCGenPlugin(TLCGenPluginElems.PlugMessaging | TLCGenPluginElems.TabControl | TLCGenPluginElems.XMLNodeWriter | TLCGenPluginElems.HasSettings)]
    [CCOLCodePieceGenerator]
    public class DynamischHiaatPlugin : CCOLCodePieceGeneratorBase, ITLCGenPlugMessaging, ITLCGenTabItem, ITLCGenXMLNodeWriter, ITLCGenHasSettings
    {
        #region Fields

        private ControllerModel _controller;
        private const string _myName = "Dynamisch hiaat";
        private DynamischHiaatModel _myModel;
        private DynamischHiaatPluginTabViewModel _myTabViewModel;
        private string _mmk;

        #endregion Fields

        #region Properties

        public DynamischHiaatDefaultsModel MyDefaults { get; private set; }
        #endregion // Properties

        #region ITLCGen shared items

        public ControllerModel Controller
        {
            get => _controller;
            set
            {
                _controller = value;
                if (_controller == null)
                {
                    _myModel = new DynamischHiaatModel();
                    _myTabViewModel.Controller = null;
                    _myTabViewModel.Model = _myModel;
                }
                if (_controller != null && _myModel != null)
                {
                    _myTabViewModel.Controller = _controller;
                }
                UpdateModel();
            }
        }

        public string GetPluginName()
        {
            return _myName;
        }

        #endregion // ITLCGen shared items

        #region ITLCGenPlugMessaging

        public void UpdateTLCGenMessaging()
        {
            _myTabViewModel.UpdateTLCGenMessaging();
        }

        #endregion // ITLCGenPlugMessaging

        #region ITLCGenTabItem

        public string DisplayName => _myName;

        public System.Windows.Media.ImageSource Icon => null;

        DataTemplate _ContentDataTemplate;
        public DataTemplate ContentDataTemplate
        {
            get
            {
                if (_ContentDataTemplate == null)
                {
                    _ContentDataTemplate = new DataTemplate();
                    var tab = new FrameworkElementFactory(typeof(DynamischHiaatPluginTabView));
                    tab.SetValue(FrameworkElement.DataContextProperty, _myTabViewModel);
                    _ContentDataTemplate.VisualTree = tab;
                }
                return _ContentDataTemplate;
            }
        }

        public bool IsEnabled { get; set; }
        
        public bool Visibility { get; set; }

        public bool CanBeEnabled()
        {
            return true;
        }

        public void LoadTabs()
        {
            
        }

        public void OnDeselected()
        {
            
        }

        public bool OnDeselectedPreview()
        {
            return true;
        }

        public void OnSelected()
        {
            if(_myTabViewModel.SelectedDynamischHiaatSignalGroup == null && _myTabViewModel.DynamischHiaatSignalGroups.Any())
            {
                _myTabViewModel.SelectedDynamischHiaatSignalGroup = _myTabViewModel.DynamischHiaatSignalGroups[0];
            }
        }

        public bool OnSelectedPreview()
        {
            return true;
        }

        #endregion ITLCGenTabItem

        #region ITLCGenXMLNodeWriter

        public void GetXmlFromDocument(XmlDocument document)
        {
            _myModel = null;

            foreach (XmlNode node in document.FirstChild.ChildNodes)
            {
                if (node.LocalName == "DynamischHiaat")
                {
                    _myModel = XmlNodeConverter.ConvertNode<DynamischHiaatModel>(node);
                    break;
                }
            }

            if (_myModel == null)
            {
                _myModel = new DynamischHiaatModel();
            }
            _myTabViewModel.Model = _myModel;
            _myTabViewModel.RaisePropertyChanged("");
        }

        public void SetXmlInDocument(XmlDocument document)
        {
            var doc = TLCGenSerialization.SerializeToXmlDocument(_myModel);
            var node = document.ImportNode(doc.DocumentElement, true);
            document.DocumentElement.AppendChild(node);
        }

        #endregion // ITLCGenXMLNodeWriter

        #region ITLCGenHasSettings

        public void LoadSettings()
        {
            MyDefaults =
                TLCGenSerialization.DeSerializeData<DynamischHiaatDefaultsModel>(
                    ResourceReader.GetResourceTextFile("TLCGen.Plugins.DynamischHiaat.Settings.DynamischHiaatDefaults.xml", this));

            var appdatpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var setpath = Path.Combine(appdatpath, @"TLCGen\DynamischHiaat\");
            if (!Directory.Exists(setpath))
                Directory.CreateDirectory(setpath);
            var setfile = Path.Combine(setpath, @"defaults.xml");

            if (File.Exists(setfile))
            {
                MyDefaults = TLCGenSerialization.DeSerialize<DynamischHiaatDefaultsModel>(setfile);
            }
            else
            {
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            // saving not needed: will never change from inside the application
            //var appdatpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //var setpath = Path.Combine(appdatpath, @"TLCGen\DynamischHiaat\");
            //if (!Directory.Exists(setpath))
            //    Directory.CreateDirectory(setpath);
            //var setfile = Path.Combine(setpath, @"defaults.xml");
            //
            //TLCGenSerialization.Serialize(setfile, MyDefaults);
        }

        #endregion // ITLCGenHasSettings

        #region CCOLCodePieceGenerator

        public override void CollectCCOLElements(ControllerModel c)
        {
            _myElements = new List<CCOLElement>();

            var warning = false;
            foreach (var msg in _myModel.SignaalGroepenMetDynamischHiaat.Where(x => x.HasDynamischHiaat))
            {
                var ofc = c.Fasen.FirstOrDefault(x => x.Naam == msg.SignalGroupName);
                if (ofc.AantalRijstroken > 1 && !ofc.ToepassenMK2 && !warning)
                {
                    warning = true;
                    TLCGenDialogProvider.Default.ShowMessageBox(
                        $"Let op!\n\n" +
                        $"Voor fase {ofc.Naam} met {ofc.AantalRijstroken} rijstroken is toepassen van Meetkriterium2() uitgeschakeld.\n" +
                        $"Dynamische hiaattijden zijn hier voor juist werking van afhankelijk.",
                        "Foutieve signaalgroep instellingen", MessageBoxButton.OK);
                }

                _myElements.Add(new CCOLElement($"dynhiaat{msg.SignalGroupName}", 1, CCOLElementTimeTypeEnum.SCH_type, CCOLElementTypeEnum.Schakelaar, $"Toepassen dynamsich hiaat bij fase {msg.SignalGroupName}"));
                _myElements.Add(new CCOLElement($"opdrempelen{msg.SignalGroupName}", CCOLElementTypeEnum.HulpElement, $"Opdrempelen toepassen voor fase {msg.SignalGroupName}"));
                _myElements.Add(new CCOLElement($"opdrempelen{msg.SignalGroupName}", msg.Opdrempelen ? 1 : 0, CCOLElementTimeTypeEnum.SCH_type, CCOLElementTypeEnum.Schakelaar, $"Opdrempelen toepassen voor fase {msg.SignalGroupName}"));
                _myElements.Add(new CCOLElement($"geendynhiaat{msg.SignalGroupName}", CCOLElementTypeEnum.HulpElement, "Tegenhouden toepassen dynamische hiaattijden voor fase " + msg.SignalGroupName));
                _myElements.Add(new CCOLElement($"edkop_{msg.SignalGroupName}", msg.KijkenNaarKoplus ? 1 : 0, CCOLElementTimeTypeEnum.SCH_type, CCOLElementTypeEnum.Schakelaar, $"Start timers dynamische hiaat fase {msg.SignalGroupName} op einde detectie koplus"));
                foreach(var d in msg.DynamischHiaatDetectoren)
                {
                    _myElements.Add(new CCOLElement($"{d.DetectorName}_1", d.Moment1, CCOLElementTimeTypeEnum.TE_type, CCOLElementTypeEnum.Timer, $"Dynamische hiaattijden moment 1 voor detector {d.DetectorName}"));
                    _myElements.Add(new CCOLElement($"{d.DetectorName}_2", d.Moment2, CCOLElementTimeTypeEnum.TE_type, CCOLElementTypeEnum.Timer, $"Dynamische hiaattijden moment 2 voor detector {d.DetectorName}"));
                    _myElements.Add(new CCOLElement($"tdh_{d.DetectorName}_1", d.TDH1, CCOLElementTimeTypeEnum.TE_type, CCOLElementTypeEnum.Timer, $"Dynamische hiaattijden TDH 1 voor detector {d.DetectorName}"));
                    _myElements.Add(new CCOLElement($"tdh_{d.DetectorName}_2", d.TDH2, CCOLElementTimeTypeEnum.TE_type, CCOLElementTypeEnum.Timer, $"Dynamische hiaattijden TDH 2 voor detector {d.DetectorName}"));
                    _myElements.Add(new CCOLElement($"max_{d.DetectorName}", d.Maxtijd, CCOLElementTimeTypeEnum.TE_type, CCOLElementTypeEnum.Timer, $"Dynamische hiaattijden maximale tijd 2 voor detector {d.DetectorName}"));
                    _myElements.Add(new CCOLElement($"verleng_{d.DetectorName}", CCOLElementTypeEnum.HulpElement, $"Instructie verlengen op detector {d.DetectorName} ongeacht dynamische hiaat"));
                    var schprm = 0;
                    if (d.SpringStart) schprm += 0x01;
                    if (d.VerlengNiet) schprm += 0x02;
                    if (d.VerlengExtra) schprm += 0x04;
                    if (d.DirectAftellen) schprm += 0x08;
                    if (d.SpringGroen) schprm += 0x10;
                    _myElements.Add(new CCOLElement($"springverleng_{d.DetectorName}", schprm, CCOLElementTimeTypeEnum.None, CCOLElementTypeEnum.Parameter, $"Dyn. hiaattij instelling voor det. {d.DetectorName} (via bitsturing)"));
                }
            }
        }

        public override bool HasCCOLElements() => true;

        public override IEnumerable<CCOLLocalVariable> GetFunctionLocalVariables(ControllerModel c, CCOLCodeTypeEnum type)
        {
            if (!_myModel.SignaalGroepenMetDynamischHiaat.Any(x => x.HasDynamischHiaat)) return base.GetFunctionLocalVariables(c, type);
            return type switch
            {
                CCOLCodeTypeEnum.RegCMeetkriterium => new List<CCOLLocalVariable>{new("int", "d", defineCondition:"(defined TDHAMAX)")},
                _ => base.GetFunctionLocalVariables(c, type)
            };
        }

        public override int[] HasCode(CCOLCodeTypeEnum type)
        {
            switch (type)
            {
                case CCOLCodeTypeEnum.RegCIncludes: return new []{115};
                case CCOLCodeTypeEnum.RegCInitApplication: return new []{115};
                case CCOLCodeTypeEnum.RegCPreApplication: return new []{115};
                case CCOLCodeTypeEnum.RegCMeetkriterium: return new []{9, 115};
                case CCOLCodeTypeEnum.SysHDefines: return new []{115};
                default:
                    return null;
            }
        }

        public override string GetCode(ControllerModel c, CCOLCodeTypeEnum type, string ts, int order)
        {
            var sb = new StringBuilder();

            var sgs = _myModel.SignaalGroepenMetDynamischHiaat.Where(x => x.HasDynamischHiaat);
            if (!sgs.Any()) return "";

            switch (type)
            {
                case CCOLCodeTypeEnum.SysHDefines:
                    //if (c.Data.CCOLVersie >= CCOLVersieEnum.CCOL110 && c.Data.TDHAMaxToepassen)
                    //{
                    //    sb.AppendLine($"#define TDHAMAX /* gebruik van TDHA_max[] */");
                    //}
                    return sb.ToString();

                case CCOLCodeTypeEnum.RegCIncludes:
                    sb.AppendLine($"{ts}#include \"dynamischhiaat.c\"");
                    return sb.ToString();
                
                case CCOLCodeTypeEnum.RegCInitApplication:
                    sb.AppendLine($"{ts}#if (CCOL_V >= 110 && !defined TDHAMAX) || (CCOL_V < 110)");
                    sb.AppendLine($"{ts}{ts}init_tdhdyn();");
                    sb.AppendLine($"{ts}#endif");
                    return sb.ToString();

                case CCOLCodeTypeEnum.RegCPreApplication:
                    sb.AppendLine($"{ts}/* Instellen basis waarde hulpelementen 'geen dynamisch hiaat gebruiken'.");
                    sb.AppendLine($"{ts}   Dit hulpelement kan in gebruikers code worden gebruikt voor eigen aansturing. */");
                    foreach (var sg in sgs)
                    {
                        sb.AppendLine($"{ts}IH[{_hpf}geendynhiaat{sg.SignalGroupName}] = !SCH[{_schpf}dynhiaat{sg.SignalGroupName}];");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"{ts}/* Instellen basis waarde hulpelementen opdrempelen t.b.v. dynamische hiaattijden.");
                    sb.AppendLine($"{ts}   Dit hulpelement kan in gebruikers code worden gebruikt voor eigen aansturing. */");
                    foreach (var sg in sgs)
                    {
                        sb.AppendLine($"{ts}IH[{_hpf}opdrempelen{sg.SignalGroupName}] = SCH[{_schpf}opdrempelen{sg.SignalGroupName}];");
                    }
                    return sb.ToString();

                case CCOLCodeTypeEnum.RegCMeetkriterium:
                    switch (order)
                    {
                        case 9:
                            //if (c.Data.CCOLVersie >= CCOLVersieEnum.CCOL110 && c.Data.TDHAMaxToepassen)
                            //{
                            //    sb.AppendLine("#ifdef TDHAMAX");
                            //    sb.AppendLine($"{ts}/* TDH_max vullen bij gebruik van TDHAMAX */");
                            //    sb.AppendLine($"{ts}for (d = 0; d < DP_MAX; ++d)");
                            //    sb.AppendLine($"{ts}{{");
                            //    sb.AppendLine($"{ts}{ts}TDH_max[d] = TDHA_max[d];");
                            //    sb.AppendLine($"{ts}}}");
                            //    sb.AppendLine("#endif // TDHAMAX");
                            //}

                            break;
                        case 115:
                            foreach(var sg in sgs)
                            {
                                var ofc = c.Fasen.FirstOrDefault(x => x.Naam == sg.SignalGroupName);
                                if (ofc == null) continue;
                                sb.AppendLine($"{ts}hiaattijden_verlenging(IH[{_hpf}geendynhiaat{sg.SignalGroupName}], SCH[{_schpf}edkop_{sg.SignalGroupName}], {(c.Data.ExtraMeeverlengenInWG ? "TRUE" : "FALSE")}, {_mpf}{_mmk}{sg.SignalGroupName}, IH[{_hpf}opdrempelen{sg.SignalGroupName}], {_fcpf}{sg.SignalGroupName}, ");
                                for (var i = 0; i < ofc.AantalRijstroken; i++)
                                {
                                    foreach(var dd in sg.DynamischHiaatDetectoren)
                                    {
                                        var od = ofc.Detectoren.FirstOrDefault(x => x.Naam == dd.DetectorName);
                                        if (od == null || od.Rijstrook - 1 != i) continue;
                                        sb.AppendLine(
                                            $"{ts}{ts}{i + 1}, " +
                                            $"{_dpf}{od.Naam}, " +
                                            $"{_tpf}{dd.DetectorName}_1, " +
                                            $"{_tpf}{dd.DetectorName}_2, " +
                                            $"{_tpf}tdh_{dd.DetectorName}_1, " +
                                            $"{_tpf}tdh_{dd.DetectorName}_2, " +
                                            $"{_tpf}max_{dd.DetectorName}, " +
                                            $"{_prmpf}springverleng_{dd.DetectorName}, " +
                                            $"{_hpf}verleng_{dd.DetectorName}, ");
                                    }
                                }
                                sb.AppendLine($"{ts}{ts}END);");
                            }
                            break;
                    }
                    return sb.ToString();
                default:
                    return "";
            }
        }

        public override List<string> GetSourcesToCopy()
        {
            if (!_myModel.SignaalGroepenMetDynamischHiaat.Any(x => x.HasDynamischHiaat)) return null;
            return new List<string>
            {
                "dynamischhiaat.c"
            };
        }

        public override bool SetSettings(CCOLGeneratorClassWithSettingsModel settings)
        {
            _mmk = CCOLGeneratorSettingsProvider.Default.GetElementName("mmk");
            return base.SetSettings(settings);
        }

        #endregion // CCOLCodePieceGenerator

        #region Private Methods

        internal void UpdateModel()
        {
            if (_controller != null && _myModel != null)
            {
                foreach (var fc in Controller.Fasen)
                {
                    if (fc.Type == TLCGen.Models.Enumerations.FaseTypeEnum.Auto &&
                        _myTabViewModel.DynamischHiaatSignalGroups.All(x => x.SignalGroupName != fc.Naam))
                    {
                        var msg = new DynamischHiaatSignalGroupViewModel(new DynamischHiaatSignalGroupModel { SignalGroupName = fc.Naam });
                        msg.SelectedDefault = MyDefaults.Defaults.FirstOrDefault(x => x.Name == _myModel.TypeDynamischHiaat);
                        if (string.IsNullOrEmpty(msg.Snelheid) || !msg.SelectedDefault.Snelheden.Any(x => x.Name == msg.Snelheid))
                        {
                            if (msg.SelectedDefault != null) msg.Snelheid = msg.SelectedDefault.DefaultSnelheid;
                        }
                        _myTabViewModel.DynamischHiaatSignalGroups.Add(msg);
                    }
                }
                var rems = new List<DynamischHiaatSignalGroupViewModel>();
                foreach (var fc in _myTabViewModel.DynamischHiaatSignalGroups)
                {
                    if (Controller.Fasen.All(x => x.Naam != fc.SignalGroupName) || Controller.Fasen.Any(x => x.Naam == fc.SignalGroupName && x.Type != TLCGen.Models.Enumerations.FaseTypeEnum.Auto))
                    {
                        rems.Add(fc);
                    }
                }
                foreach (var sg in rems)
                {
                    _myTabViewModel.DynamischHiaatSignalGroups.Remove(sg);
                }
                _myTabViewModel.DynamischHiaatSignalGroups.BubbleSort();
                _myTabViewModel.RaisePropertyChanged("");
            }
        }

        #endregion // Private Methods

        #region Constructor

        public DynamischHiaatPlugin()
        {
            _myTabViewModel = new DynamischHiaatPluginTabViewModel(this);
        }

        #endregion //Constructor
    }
}
