﻿using NUnit.Framework;
using TLCGen.ViewModels;
using TLCGen.Models;
using TLCGen.DataAccess;

namespace TLCGen.UnitTests
{
    [TestFixture]
    public class FasenDetailsTabViewModelTests
    {
        [Test]
        public void FasenDetailsTabSelected_ControllerHas5Fasen_TabAlsoExposes5Fasen()
        {
            var model = new ControllerModel();
            model.Fasen.Add(new FaseCyclusModel() { Naam = "01" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "02" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "03" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "04" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "05" });
            var vm = new FasenDetailsTabViewModel();
            vm.Controller = model;
            TLCGenControllerDataProvider.OverrideDefault(FakesCreator.CreateControllerDataProvider(model));

            vm.OnSelected();

            Assert.AreEqual(5, vm.Fasen.Count);
        }

        [Test]
        public void FasenDetailsTabSelectedFase_TabDeselectedAndSelected_SelectedFaseEqual()
        {
            var model = new ControllerModel();
            model.Fasen.Add(new FaseCyclusModel() { Naam = "01" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "02" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "03" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "04" });
            model.Fasen.Add(new FaseCyclusModel() { Naam = "05" });
            var vm = new FasenDetailsTabViewModel();
            vm.Controller = model;
            TLCGenControllerDataProvider.OverrideDefault(FakesCreator.CreateControllerDataProvider(model));

            vm.OnSelected();
            vm.SelectedFaseCyclus = vm.Fasen[3];
            vm.OnDeselected();
            vm.OnSelected();

            Assert.True(object.ReferenceEquals(vm.SelectedFaseCyclus, vm.Fasen[3]));
        }
    }
}
