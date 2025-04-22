/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

18/06/2024	1.0.0.1		EPA, Skyline	Initial version
22/04/2025	1.0.0.2		DPR, Skyline	Changed Folder Location
****************************************************************************
*/

namespace DMA_UPTIME_1
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		private string subId;
		private Dictionary<string, int> uptimes;

		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			try
			{
				RunSafe(engine);
			}
			catch (ScriptAbortException)
			{
				// Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
				throw; // Comment if it should be treated as a normal exit of the script.
			}
			catch (ScriptForceAbortException)
			{
				// Catch forced abort exceptions, caused via external maintenance messages.
				throw;
			}
			catch (ScriptTimeoutException)
			{
				// Catch timeout exceptions for when a script has been running for too long.
				throw;
			}
			catch (InteractiveUserDetachedException)
			{
				// Catch a user detaching from the interactive script by closing the window.
				// Only applicable for interactive scripts, can be removed for non-interactive scripts.
				throw;
			}
			catch (Exception e)
			{
				engine.ExitFail("Run|Something went wrong: " + e);
			}
		}

		private void RunSafe(IEngine engine)
		{

			var uptimesReceived = GetDmaUptime(engine);

			List<TestResult> results = new List<TestResult>();
			foreach (var uptime in uptimesReceived)
			{
				TestResult testResult = new TestResult()
				{
					ParameterName = "DMA_UPTIME",
					DmaName = uptime.Key,
					ReceivedValue = uptime.Value.ToString(),
				};
				engine.GenerateInformation(uptime.Key);
				engine.GenerateInformation(uptime.Value.ToString());
				results.Add(testResult);
			}

			engine.AddScriptOutput("result", JsonConvert.SerializeObject(results));
		}

		private Dictionary<string, int> GetDmaUptime(IEngine engine)
		{
			subId = Guid.NewGuid().ToString();
			uptimes = new Dictionary<string, int>();
			IConnection connection = Engine.SLNetRaw;
			connection.OnNewMessage += HandleMessage;
			connection.AddSubscription(subId, new SubscriptionFilter(typeof(DataMinerPerformanceInfoEventMessage)));
			engine.Sleep(1000);
			connection.OnNewMessage -= HandleMessage;
			connection.RemoveSubscription(subId);
			return uptimes;
		}

		private void HandleMessage(object sender, NewMessageEventArgs newEvent)
		{
			if (!newEvent.FromSet(subId))
			{
				return;
			}

			if (newEvent.Message is DataMinerPerformanceInfoEventMessage performanceMessage)
			{
				TimeSpan uptime = DateTime.Now - performanceMessage.StartupTime;
				uptimes[performanceMessage.DataMinerName] = (int)uptime.TotalHours;
			}
		}

		public class TestResult
		{
			public string ParameterName { get; set; }

			public string DmaName { get; set; }

			public string ReceivedValue { get; set; }
		}
	}
}
