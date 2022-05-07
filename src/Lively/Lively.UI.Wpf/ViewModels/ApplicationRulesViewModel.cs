﻿using Lively.Common;
using Lively.Common.Helpers.MVVM;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.Wpf.Factories;
using Lively.UI.Wpf.Helpers;
using Lively.UI.Wpf.Helpers.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Documents;

namespace Lively.UI.Wpf.ViewModels
{
    public class ApplicationRulesViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool itemSelected = false;

        private readonly IUserSettingsClient userSettings;
        private readonly IApplicationsRulesFactory appRuleFactory;

        public ApplicationRulesViewModel(IUserSettingsClient userSettings, IApplicationsRulesFactory appRuleFactory)
        {
            this.userSettings = userSettings;
            this.appRuleFactory = appRuleFactory;

            AppRules = new ObservableCollection<IApplicationRulesModel>(userSettings.AppRules);
            //Localization of apprules text..
            //foreach (var item in AppRules)
            //{
            //    item.RuleText = LocalizationUtil.GetLocalizedAppRules(item.Rule);
            //}
        }

        private ObservableCollection<IApplicationRulesModel> _appRules;
        public ObservableCollection<IApplicationRulesModel> AppRules
        {
            get
            {
                return _appRules ?? new ObservableCollection<IApplicationRulesModel>();
            }
            set
            {
                _appRules = value;
                OnPropertyChanged();
            }
        }

        private IApplicationRulesModel _selectedItem;
        public IApplicationRulesModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                itemSelected = _selectedItem != null;
                RemoveCommand.RaiseCanExecuteChanged();
                if (itemSelected)
                {
                    SelectedAppRuleProperty = (int)_selectedItem.Rule;
                }
                OnPropertyChanged();
            }
        }

        
        private int _selectedAppRuleProperty;
        public int SelectedAppRuleProperty
        {
            get { return _selectedAppRuleProperty; }
            set
            {
                _selectedAppRuleProperty = value;
                if (itemSelected)
                {
                    SelectedItem.Rule = (AppRulesEnum)_selectedAppRuleProperty;
                    //SelectedItem.RuleText = LocalizationUtil.GetLocalizedAppRules(SelectedItem.Rule);
                }
                OnPropertyChanged();
            }
        }
            

        private RelayCommand _addCommand;
        public RelayCommand AddCommand
        {
            get
            {
                if (_addCommand == null)
                {
                    _addCommand = new RelayCommand(
                        param => AddProgram()
                        );
                }
                return _addCommand;
            }
        }

        private RelayCommand _removeCommand;
        public RelayCommand RemoveCommand
        {
            get
            {
                if (_removeCommand == null)
                {
                    _removeCommand = new RelayCommand(
                        param => RemoveProgram(), param => itemSelected
                        );
                }
                return _removeCommand;
            }
        }

        private void AddProgram()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "application (*.exe) |*.exe"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var rule = appRuleFactory.CreateAppRule(openFileDialog.FileName, AppRulesEnum.pause);
                    if (AppRules.Any(x => x.AppName.Equals(rule.AppName, StringComparison.Ordinal)))
                    {
                        return;
                    }
                    userSettings.AppRules.Add(rule);
                    //rule.RuleText = LocalizationUtil.GetLocalizedAppRules(rule.Rule);
                    AppRules.Add(rule);
                }
                catch (Exception)
                {
                    //todo loggin
                }
            }
        }

        private void RemoveProgram()
        {
            userSettings.AppRules.Remove(SelectedItem);
            AppRules.Remove(SelectedItem);
        }

        public void UpdateDiskFile()
        {
            _ = userSettings.SaveAsync<List<IApplicationRulesModel>>();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            //save on exit..
            UpdateDiskFile();
        }
    }
}
