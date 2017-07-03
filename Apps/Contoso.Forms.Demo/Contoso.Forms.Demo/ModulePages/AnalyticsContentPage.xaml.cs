﻿using System;
using System.Collections.Generic;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Xamarin.Forms;

namespace Contoso.Forms.Demo
{
    public class Property
    {
        public Property(string propertyName, string propertyValue)
        {
            Name = propertyName;
            Value = propertyValue;
        }
        public string Name;
        public string Value;
    }

    [Android.Runtime.Preserve(AllMembers = true)]
    public partial class AnalyticsContentPage : ContentPage
    {
        List<Property> EventProperties;

        public AnalyticsContentPage()
        {
            InitializeComponent();
            EventProperties = new List<Property>();
            NumPropertiesLabel.Text = EventProperties.Count.ToString();
            if (Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.iOS)
            {
                Icon = "lightning.png";
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnabledSwitchCell.On = Analytics.Enabled;
            EnabledSwitchCell.IsEnabled = MobileCenter.Enabled;
        }

        void AddProperty(object sender, EventArgs e)
        {
            var addPage = new AddPropertyContentPage();
            addPage.PropertyAdded += (Property property) => { 
                EventProperties.Add(property); 
                RefreshPropCount();
            };
            Navigation.PushModalAsync(addPage);
        }

        void PropertiesCellTapped(object sender, EventArgs e)
        {
            Navigation.PushAsync(new PropertiesContentPage(EventProperties));
        }

        void TrackEvent(object sender, EventArgs e)
        {
            var properties = new Dictionary<string, string>();
            foreach (Property property in EventProperties)
            {
                properties.Add(property.Name, property.Value);
            }

            if (EventProperties.Count == 0)
            {
                Analytics.TrackEvent(EventNameCell.Text);
                return;
            }

            EventProperties.Clear();
            RefreshPropCount();
            Analytics.TrackEvent(EventNameCell.Text, properties);

        }

        void UpdateEnabled(object sender, ToggledEventArgs e)
        {
            Analytics.Enabled = e.Value;
        }

        void RefreshPropCount()
        {
            NumPropertiesLabel.Text = EventProperties.Count.ToString();
        }
    }
}
