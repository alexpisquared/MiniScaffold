﻿global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.Data;
global using System.Diagnostics;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Windows;
global using System.Windows.Data;
global using System.Windows.Input;
global using System.Windows.Markup;
global using AutoMapper;
global using CI.LDAP.Lib;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using CsvHelper;
global using CsvHelper.Configuration;
global using DB.QStats.Std.Models;
global using EF.DbHelper.Lib;
global using Microsoft.Data.SqlClient;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using MinNavTpl.Commands;
global using MinNavTpl.Models;
global using MinNavTpl.Services;
global using MinNavTpl.Stores;
global using MinNavTpl.VM.Services;
global using MinNavTpl.VM.Stores;
global using MinNavTpl.VM.VMs;
global using StandardContractsLib;
global using StandardLib.Base;
global using StandardLib.Helpers;
global using WpfUserControlLib.Extensions;
global using static System.Diagnostics.Trace;
//obal using System.Text.Json; //tu: new and very performant Json lib (Dec 2021)
global using OL = Microsoft.Office.Interop.Outlook;
global using Application = System.Windows.Application;
//obal using System.Text.Json; //tu: new and very performant Json lib (Dec 2021)
