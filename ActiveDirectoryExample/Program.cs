using System;
using System.DirectoryServices.Protocols;
using Galactic.ActiveDirectory;

namespace ActiveDirectoryExample
{
    // This program is an example of how to access Active Directory and query information from various object
    // types using the Galactic API.
    class Program
    {
        // The directory containing configuration items used by the application.
        private const string CONFIG_ITEM_DIRECTORY = @"ConfigurationItems\";

        // The name of the configuration item that contains the information required to connect to Active Directory.
        private const string AD_CONFIGURATION_ITEM_NAME = "ActiveDirectory";

        static void Main(string[] args)
        {
            // ---------- ESTABLISH A CONNECTION ----------

            // First create an Active Directory object we can use to access and manage Active Directory.
            // This example uses a configuration item to hold configuration data and credentials for the
            // connection. This is recommended as it makes it easy to change this data when necessary,
            // and keeps credentials out of the code.
            // You can easily create and view configuration items with the Galactic API Configuration Item
            // manager available at http://galacticapi.github.io. There are also templates there on the
            // syntax to use when creating configuration items for Active Directory.
            // Queries using this object default to using the root of the directory, unless otherwise
            // specified in the configuration item.
            //
            // An example Active Directory configuration item is provided below:
            // 
            // Syntax:
            // domain_name[:site_name]
            // service_account_name
            // service_account_password
            //
            // Example:
            // galacticapi.com:Default-First-Site-Name
            // galacticUser
            // 0rion$Belt
            //
            // Note: The Active Directory site name is optional. It's used to limit the API's search for
            // domain controllers to just within the specified site. This is useful if you have domain
            // controllers across a WAN and don't want the API to used a distant domain controller for its
            // connections.
            ActiveDirectory ad = new ActiveDirectory(AppDomain.CurrentDomain.BaseDirectory + CONFIG_ITEM_DIRECTORY, AD_CONFIGURATION_ITEM_NAME);

            // We'll write a message to the console indicating some basic information about the connection.
            Console.WriteLine("Connected to " + ad.Name + " (" + ad.NTName + ") directory.");
            Console.WriteLine();

            // ---------- QUERY USER INFORMATION ----------

            // Now let's retrieve some information about a user. The Galactic API deals entirely in Active
            // Directory GUIDs. This is important when manipulating objects, as names can change, but GUIDs
            // do not. As such we make a call to get the GUID of the user based upon their sAMAccountName below.
            // Change USERNAME below, to the sAMAccountName of a user in Active Directory. The Active Directory
            // object has other methods to get the GUID of an object based upon other attributes of the object
            // such as it's distinguished name, employee number, and common name.
            string userSAMAccountName = "USERNAME";
            User user = new User(ad, ad.GetGUIDBySAMAccountName(userSAMAccountName));

            // Let's write out some information about the user.
            Console.WriteLine("Found user at: " + user.DistinguishedName);
            Console.WriteLine("\tSAM Account Name: " + user.SAMAccountName);
            Console.WriteLine("\tFirst Name: " + user.FirstName);
            Console.WriteLine("\tLast Name: " + user.LastName);
            // Let's write out some password / security related information.
            Console.WriteLine("\tAccount Disabled?: " + user.IsDisabled);
            Console.WriteLine("\tPassword Expired?: " + user.PasswordExpired);
            Console.WriteLine("\tLast time the password was set: " + user.PasswordLastSet);
            Console.WriteLine("\tNumber of bad passwords entered: " + user.BadPasswordCount);
            Console.WriteLine("\tLast time a bad password was entered: " + user.BadPasswordTime);
            // Let's write out the groups that the user belongs to.
            Console.WriteLine(("\tMember of:"));
            foreach (string usersGroup in user.Groups)
            {
                Console.WriteLine("\t\t" + usersGroup);
            }
            // If you have a Microsoft Exchange system integrated with your Active Directory these may be
            // useful to you.
            // Primary e-mail address works without Exchange as well. It uses the mail attribute then.
            Console.WriteLine("\tPrimary E-mail Address: " + user.PrimaryEmailAddress);
            // Let's list all of the e-mail addresses associated with the user.
            Console.WriteLine("\tAll e-mail addresses:");
            foreach (string emailAddress in user.EmailAddresses)
            {
                Console.WriteLine("\t\t" + emailAddress);
            }
            Console.WriteLine();

            // ---------- QUERY GROUP INFORMATION ----------

            // Now let's retrieve some information about a group. The syntax is similar to what we used with
            // users above. Change GROUPNAME below, to the sAMAccountName of a group in Active Directory.
            string groupSAMAccountName = "GROUPNAME";
            Group group = new Group(ad, ad.GetGUIDBySAMAccountName(groupSAMAccountName));

            // Let's write out some information about the group.
            Console.WriteLine("Found group at: " + group.DistinguishedName);
            Console.WriteLine("\tSAM Account Name: " + group.SAMAccountName);
            // Let's write out the members of the group, and what type of object they are.
            Console.WriteLine("\tMembers:");
            // Active Directory Security Principals are objects in AD that can be authenticated by the system
            // as well as groups that the system uses to determine membership. In the Galactic API, Group and
            // User objects derive from SecurityPrincipals, so they are a good base class to use when you need
            // to deal with objects of both types. The Members property of group below returns SecurityPrincipals
            // which we'll then test to determine their type.
            foreach (SecurityPrincipal principal in group.Members)
            {
                if (principal.IsUser)
                {
                    Console.WriteLine("\t\t" + principal.SAMAccountName + " - User");
                }
                else // The principal is a group... principal.IsGroup could be used to check this.
                {
                    Console.WriteLine("\t\t" + principal.SAMAccountName + " - Group");
                }
            }
            Console.WriteLine();

            // ---------- USING LDAP QUERIES ----------

            // If you really need to find an object in AD using a LDAP query, the Galactic API supports this as well.
            // Just supply the filter you'd like to use, and a list of the attributes to return with the call. If you
            // omit the list of attributes, all attributes will be returned. You can use GetEntry to return the first
            // (or only) result found, or GetEntries to return them all.
            // These methods return System.DirectoryServices.Protocols.SearchResultEntry objects so be sure to include
            // the DLL for the library in your program, as I have above.
            // The example filter below will return an object with the supplied sAMAccountName. Change SAMACCOUNTNAME
            // below, to the sAMAccountName of an object in Active Directory.
            string filterSAMAccountName = "SAMACCOUNTNAME";
            SearchResultEntry entry = ad.GetEntry("(sAMAccountName=" + filterSAMAccountName + ")");

            // We can check if an object with the supplied SAM Account Name was found.
            if (entry != null)
            {
                // Let's write some basic information about the object found.
                Console.WriteLine("Found object with sAMAccountName " + filterSAMAccountName + " at " + entry.DistinguishedName);
            }

            // Let's say that this entry corresponds with a user. We could then use the information we retrieved from the
            // LDAP query to create a new user object in the Galactic API, that we could use to more easily access and
            // manipulate its attributes.
            User queriedUser = new User(ad, ad.GetGUIDByDistinguishedName(entry.DistinguishedName));

            // ---------- THANKS! ----------
            // I hope this has be informative. For more information and other examples how to use the Galactic API please
            // visit http://galacticapi.github.io.
            Console.WriteLine();
            Console.WriteLine("Thanks for using the program!");
            Console.WriteLine("Press enter to end the program.");
            Console.ReadLine();
        }
    }
}
