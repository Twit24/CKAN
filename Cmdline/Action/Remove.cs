﻿using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CKAN.CmdLine
{
    public class Remove : ICommand
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Remove));

        public IUser user { get; set; }
        private KSPManager manager;

        /// <summary>
        /// Initialize the remove command object
        /// </summary>
        /// <param name="mgr">KSPManager containing our instances</param>
        /// <param name="user">IUser object for interaction</param>
        public Remove(KSPManager mgr, IUser user)
        {
            manager   = mgr;
            this.user = user;
        }

        /// <summary>
        /// Uninstalls a module, if it exists.
        /// </summary>
        /// <param name="ksp">Game instance from which to remove</param>
        /// <param name="raw_options">Command line options object</param>
        /// <returns>
        /// Exit code for shell environment
        /// </returns>
        public int RunCommand(CKAN.KSP ksp, object raw_options)
        {

            RemoveOptions options = (RemoveOptions) raw_options;

            // Use one (or more!) regex to select the modules to remove
            if (options.regex)
            {
                log.Debug("Attempting Regex");
                // Parse every "module" as a grumpy regex
                var justins = options.modules.Select(s => new Regex(s));

                // Modules that have been selected by one regex
                List<string> selectedModules = new List<string>();

                // Get the list of installed modules
                IRegistryQuerier registry = RegistryManager.Instance(ksp).registry;

                // Try every regex on every installed module:
                // if it matches, select for removal
                foreach (string mod in registry.InstalledModules.Select(mod => mod.identifier))
                {
                    if (justins.Any(re => re.IsMatch(mod)))
                        selectedModules.Add(mod);
                }

                // Replace the regular expressions with the selected modules
                // and continue removal as usual
                options.modules = selectedModules;
            }

            if (options.rmall)
            {
                log.Debug("Removing all mods");
                // Add the list of installed modules to the list that should be uninstalled
                IRegistryQuerier registry = RegistryManager.Instance(ksp).registry;
                options.modules.AddRange(
                    registry.InstalledModules.Select(mod => mod.identifier)
                );
            }

            if (options.modules != null && options.modules.Count > 0)
            {
                try
                {
                    var installer = ModuleInstaller.GetInstance(ksp, manager.Cache, user);
                    Search.AdjustModulesCase(ksp, options.modules);
                    installer.UninstallList(options.modules);
                }
                catch (ModNotInstalledKraken kraken)
                {
                    user.RaiseMessage("I can't do that, {0} isn't installed.", kraken.mod);
                    user.RaiseMessage("Try `ckan list` for a list of installed mods.");
                    return Exit.BADOPT;
                }
            }
            else
            {
                user.RaiseMessage("No mod selected, nothing to do");
                return Exit.BADOPT;
            }

            return Exit.OK;
        }
    }
}
