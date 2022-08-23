using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;

namespace Animatroller.Common
{
    public static class ArgumentParser
    {
        public class AddressId
        {
            public IPAddress Address { get; set; }

            public bool AddressIsMulticast { get; set; }

            public int? UniverseId { get; set; }

            public override string ToString()
            {
                string addressPart = Address != null ? Address.ToString() : (AddressIsMulticast ? "*" : "");
                string idPart = UniverseId != null ? UniverseId.ToString() : "*";

                return $"{addressPart}/{idPart}";
            }
        }

        public static AddressId ParseAddressAndUniverseId(string inputString, Action<string> errorReporter)
        {
            var inputParts = inputString.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            IPAddress address = null;
            bool multicast = true;
            int? universeId = null;
            if (inputParts.Length == 1)
            {
                if (inputParts[0].Count(x => x == '.') == 3)
                {
                    // IP address
                    if (!IPAddress.TryParse(inputParts[0], out address))
                    {
                        // Ignore
                        errorReporter?.Invoke($"Invalid address: {inputParts[0]}");

                        return null;
                    }
                }
                else
                {
                    // Only universe id
                    if (!int.TryParse(inputParts[0], out int id) || id < 1 || id > 63999)
                    {
                        // Ignore
                        errorReporter?.Invoke($"Invalid universe: {inputParts[0]}");

                        return null;
                    }
                    universeId = id;
                }
            }
            else if (inputParts.Length == 2)
            {
                // Address and Universe id
                if (!string.IsNullOrEmpty(inputParts[0]))
                {
                    if (inputParts[0] == "*")
                    {
                        multicast = false;
                    }
                    else if (!IPAddress.TryParse(inputParts[0], out address))
                    {
                        // Ignore
                        errorReporter?.Invoke($"Invalid address: {inputParts[0]}");

                        return null;
                    }
                }

                if (!int.TryParse(inputParts[1], out int id) || id < 1 || id > 63999)
                {
                    // Ignore
                    errorReporter?.Invoke($"Invalid universe: {inputParts[1]}");

                    return null;
                }
                universeId = id;
            }

            return new AddressId
            {
                Address = address,
                UniverseId = universeId,
                AddressIsMulticast = multicast
            };
        }
    }
}
