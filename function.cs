#r "Microsoft.ServiceBus"
#r "Newtonsoft.Json"

using System;
using System.Dynamic;
using System.Text;
using Microsoft.ServiceBus.Messaging;

public static string Run(EventData myEventHubMessage, TraceWriter log)
{
  dynamic msgObject = new ExpandoObject();
  var deviceId = GetDeviceId(myEventHubMessage);
  double[] values = tempDecode(myEventHubMessage.GetBytes());
  log.Info($"deviceId: { deviceId }, Object Temperature: {values[0]}, Ambient Temperature: {values[1]}");

  msgObject.deviceId = deviceId;
  msgObject.objTemperature = values[0];
  msgObject.ambTemperature = values[1];
	
  return Newtonsoft.Json.JsonConvert.SerializeObject(msgObject);
}

private static string GetDeviceId(EventData message)
{
  return message.SystemProperties["iothub-connection-device-id"].ToString();
}

public static double[] tempDecode(byte[] bArray)
{
	double[] output = {.0, .0};
	const double SCALE_LSB = 0.03125;
	double t;
	int it;
	
	if(bArray.Length != 4)
	{
		return output;
	}
	
	UInt16 rawAmbTemp = (UInt16)(((UInt16)bArray[3] << 8) + (UInt16)bArray[2]);
	UInt16 rawObjTemp = (UInt16)(((UInt16)bArray[1] << 8) + (UInt16)bArray[0]);

	it = (int)((rawObjTemp) >> 2);
	t = ((double)(it)) * SCALE_LSB;
	output[0] = t;
	
	it = (int)((rawAmbTemp) >> 2);
	t = (double)it;
	output[1] = t * SCALE_LSB;
	return output;
}
