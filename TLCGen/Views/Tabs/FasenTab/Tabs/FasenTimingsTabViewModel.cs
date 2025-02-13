﻿using TLCGen.Helpers;
using System;
using TLCGen.Messaging.Messages;
using System.Linq;
using System.Windows.Media;
using TLCGen.Extensions;
using TLCGen.ModelManagement;
using TLCGen.Models;
using TLCGen.Plugins;
using TimingsFaseCyclusDataModel = TLCGen.Models.TimingsFaseCyclusDataModel;

namespace TLCGen.ViewModels
{
    [TLCGenTabItem(index: -1, type: TabItemTypeEnum.FasenTab)]
    public class FasenTimingsTabViewModel : TLCGenTabItemViewModel
    {
        #region Fields

        private TimingsFaseCyclusDataViewModel _selectedTimingsFase;
        private string _betaMsgId;
        private ControllerModel _controller;

        #endregion // Fields

        #region Properties

        public override string DisplayName => "Timings";
        
        public ImageSource Icon => null;
        
        public TimingsFaseCyclusDataViewModel SelectedTimingsFase
        {
            get => _selectedTimingsFase;
            set
            {
                _selectedTimingsFase = value;
                RaisePropertyChanged();
            }
        }

        public override ControllerModel Controller
        {
            get => _controller;
            set
            {
                _controller = value;
                if (_controller != null)
                {
                    TimingsFasen = new ObservableCollectionAroundList<TimingsFaseCyclusDataViewModel, TimingsFaseCyclusDataModel>(_controller.TimingsData.TimingsFasen);
                }
                RaisePropertyChanged();
            }
        }

        public ObservableCollectionAroundList<TimingsFaseCyclusDataViewModel, TimingsFaseCyclusDataModel> TimingsFasen { get; private set; }

        public bool TimingsToepassen
        {
            get => _controller.TimingsData.TimingsToepassen;
            set
            {
                _controller.TimingsData.TimingsToepassen = value;
                RaisePropertyChanged<object>(broadcast: true);
                RaisePropertyChanged(nameof(TimingsToepassenOK));
            }
        }
        
        public bool TimingsUsePredictions
        {
            get => _controller.TimingsData.TimingsUsePredictions;
            set
            {
                _controller.TimingsData.TimingsUsePredictions = value;
                RaisePropertyChanged<object>(broadcast: true);
                if (value)
                {
                    _betaMsgId = Guid.NewGuid().ToString();
                    var msg = new ControllerAlertMessage(_betaMsgId)
                    {
                        Background = Brushes.Lavender,
                        Shown = true,
                        Message = "***Let op!*** Timings voorspellingen functiontionaliteit bevindt zich in de bèta test fase.",
                        Type = ControllerAlertType.FromPlugin
                    };
                    TLCGenModelManager.Default.AddControllerAlert(msg);
                }
                else
                {
                    TLCGenModelManager.Default.RemoveControllerAlert(_betaMsgId);
                }
            }
        }

        public bool TimingsToepassenAllowed => _controller.Data.CCOLVersie >= TLCGen.Models.Enumerations.CCOLVersieEnum.CCOL9;
        
        public bool TimingsPredictionsToepassenAllowed => _controller.TimingsData.TimingsToepassen && _controller.Data.CCOLVersie >= Models.Enumerations.CCOLVersieEnum.CCOL110;

        public bool TimingsToepassenOK => TimingsToepassenAllowed && TimingsToepassen;

        #endregion // Properties

        #region Commands

        #endregion // Commands

        #region TLCGen messaging

        private void OnFasenChanged(FasenChangedMessage msg)
        {
            if(msg.RemovedFasen != null && msg.RemovedFasen.Any())
            {
                foreach(var fc in msg.RemovedFasen)
                {
                    var TimingsFc = TimingsFasen.FirstOrDefault(x => x.FaseCyclus == fc.Naam);
                    if(TimingsFc != null)
                    {
                        TimingsFasen.Remove(TimingsFc);
                    }
                }
            }
            if (msg.AddedFasen != null && msg.AddedFasen.Any())
            {
                foreach (var fc in msg.AddedFasen)
                {
                    var Timingsfc = new TimingsFaseCyclusDataViewModel(
                                new TimingsFaseCyclusDataModel { FaseCyclus = fc.Naam });
                    TimingsFasen.Add(Timingsfc);
                }
            }
            TimingsFasen.BubbleSort();
        }

        #endregion // TLCGen messaging

        #region Private Methods 

        #endregion // Private Methods 

        #region Public Methods

        public void UpdateMessaging()
        {
            MessengerInstance.Register<FasenChangedMessage>(this, OnFasenChanged);
        }

        #endregion // Public Methods
    }
}
