﻿using System;
using System.IO;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Crashes;

namespace Contoso.Android.Puppet
{
    public class CrashesFragment : PageFragment
    {
        private Switch CrashesEnabledSwitch;
        private Button TestCrashButton;
        private Button DivideByZeroButton;
        private Button CrashWithAggregateExceptionButton;
        private Button CrashWithNullReferenceExceptionButton;
        private Button CatchNullReferenceExceptionButton;
        private Button CrashAsyncButton;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.Crashes, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            // Find views.
            CrashesEnabledSwitch = view.FindViewById(Resource.Id.enabled_crashes) as Switch;
            TestCrashButton = view.FindViewById(Resource.Id.test_crash) as Button;
            DivideByZeroButton = view.FindViewById(Resource.Id.divide_by_zero) as Button;
            CrashWithAggregateExceptionButton = view.FindViewById(Resource.Id.crash_with_aggregate_exception) as Button;
            CrashWithNullReferenceExceptionButton = view.FindViewById(Resource.Id.crash_with_null_reference_exception) as Button;
            CatchNullReferenceExceptionButton = view.FindViewById(Resource.Id.catch_null_reference_exception) as Button;
            CrashAsyncButton = view.FindViewById(Resource.Id.crash_async) as Button;

            // Subscribe to events.
            CrashesEnabledSwitch.CheckedChange += UpdateEnabled;
            TestCrashButton.Click += TestCrash;
            DivideByZeroButton.Click += DivideByZero;
            CrashWithAggregateExceptionButton.Click += CrashWithAggregateException;
            CrashWithNullReferenceExceptionButton.Click += CrashWithNullReferenceException;
            CatchNullReferenceExceptionButton.Click += CatchNullReferenceException;
            CrashAsyncButton.Click += CrashAsync;

            UpdateState();
        }

        protected override void UpdateState()
        {
            CrashesEnabledSwitch.CheckedChange -= UpdateEnabled;
            CrashesEnabledSwitch.Enabled = true;
            CrashesEnabledSwitch.Checked = Crashes.Enabled;
            CrashesEnabledSwitch.Enabled = MobileCenter.Enabled;
            CrashesEnabledSwitch.CheckedChange += UpdateEnabled;
        }

        private void UpdateEnabled(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Crashes.Enabled = e.IsChecked;
            CrashesEnabledSwitch.Checked = Crashes.Enabled;
        }

        private void TestCrash(object sender, EventArgs e)
        {
            Crashes.GenerateTestCrash();
        }

        private void DivideByZero(object sender, EventArgs e)
        {
            /* This is supposed to cause a crash, so we don't care that the variable 'x' is never used */
#pragma warning disable CS0219
            int x = (42 / int.Parse("0"));
#pragma warning restore CS0219
        }

        private void CatchNullReferenceException(object sender, EventArgs e)
        {
            try
            {
                TriggerNullReferenceException();
            }
            catch (NullReferenceException)
            {
                System.Diagnostics.Debug.WriteLine("null reference exception");
            }
        }

        private void CrashWithNullReferenceException(object sender, EventArgs e)
        {
            TriggerNullReferenceException();
        }

        private void TriggerNullReferenceException()
        {
            string[] values = { "one", null, "two" };
            for (int ctr = 0; ctr <= values.GetUpperBound(0); ctr++)
            {
                var val = values[ctr].Trim();
                var separator = ctr == values.GetUpperBound(0) ? "" : ", ";
                System.Diagnostics.Debug.WriteLine("{0}{1}", val, separator);
            }
            System.Diagnostics.Debug.WriteLine("");
        }

        private void CrashWithAggregateException(object sender, EventArgs e)
        {
            throw PrepareException();
        }

        async private void CrashAsync(object sender, EventArgs e)
        {
            await FakeService.DoStuffInBackground();
        }

        static Exception PrepareException()
        {
            try
            {
                throw new AggregateException(SendHttp(), new ArgumentException("Invalid parameter", ValidateLength()));
            }
            catch (Exception e)
            {
                return e;
            }
        }

        static Exception SendHttp()
        {
            try
            {
                throw new IOException("Network down");
            }
            catch (Exception e)
            {
                return e;
            }
        }

        static Exception ValidateLength()
        {
            try
            {
                throw new ArgumentOutOfRangeException(null, "It's over 9000!");
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
