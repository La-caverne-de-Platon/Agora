﻿using System;

namespace Agora {
    internal class Program {

        /// <summary>
        /// La discussion en entier
        /// </summary>
        public static Discussion discussion;

        
        static void Main(string[] args)
        {  

            discussion = new Discussion();
            discussion.InitialiserAuteurs();
            discussion.InitializerMessages();

            while(true)
                discussion.Continuer();
        }
    }
}