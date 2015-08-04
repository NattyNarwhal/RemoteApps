This is a small tool that right now fetches the information for RemoteApps.
As of right now, it only fetches an icon and RDP file. In the future, it would
be very useful for Unix users - it can fetch RemoteApps, and create .desktop
files for seamless Windows applications.

# Notes

RemoteApps are accessed via a URI like `https://rdweb.domain.com/RDWeb/Feed/webfeed.aspx`. Inside of this is a form-encoded XML file with the schema `http://schemas.microsoft.com/ts/2007/05/tswf`.
