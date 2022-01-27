﻿using System;
using Liminal.Platform.Experimental.App.Experiences;
using Experience = Liminal.Platform.Experimental.App.Experiences.Experience;

namespace Liminal.Platform.Experimental.Exceptions
{
    public class BundleFileException : Exception
    {
        private const string DefaultMessage = "An ExperienceApp component was not found";

        /// <summary>
        /// Gets the <see cref="App.Experiences.Experience"/> the exception relates to.
        /// </summary>
        public App.Experiences.Experience Experience { get; private set; }

        public BundleFileException(App.Experiences.Experience experience) : this(experience, DefaultMessage)
        {
            Experience = experience;
        }

        public BundleFileException(App.Experiences.Experience experience, Exception innerException) : base(DefaultMessage, innerException)
        {
            Experience = experience;
        }

        public BundleFileException(App.Experiences.Experience experience, string message) : base(message)
        {
            Experience = experience;
        }
    }
}