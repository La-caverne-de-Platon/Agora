using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Agora {
    internal class Outils {

        public static string TypologieAuHasard(List<string> list)
        {
            if (list.Count == 0)
            {
                throw new ArgumentException("The list is empty.");
            }

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);

                // Convert bytes to an integer index within the list's range.
                int index = (int)(BitConverter.ToUInt32(randomBytes, 0) % (uint)list.Count);

                return list[index];
            }
        }

    }
}
