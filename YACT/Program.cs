using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using CriCpkMaker;

namespace YACpkTool
{
    class Program
    {
        const byte NO_ARGS     = 0x01;
        const byte WRONG_USAGE = 0x02;
        const byte HELP_INFO   = 0x04;

        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                PrintUsage(NO_ARGS);
                return;
            }

            bool bList = false; // Old method, TODO update
            bool bVerbose = false;
            bool doExtract = false;
            bool doPack = false;
            bool doReplace = false;
            bool _doList = false; // Special case
            string inFileName = null;
            string outFileName = null;
            string workingDir = Directory.GetCurrentDirectory();
            string packCompressCodec = null;
            string packDataAlign = null;
            string extractWhat = null;
            string replaceWhat = null;
            string replaceWith = null;

    // STEP 1 - PARSE THE ARGUMENTS
            byte usageHelpFlags = 0x00;
            bool isUsageSimple = (args[0][0] != '-');
            if (!isUsageSimple) // Indicates technical usage vs simple usage; first argument must determine this
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string option = args[i];
                    if (option[0] == '-')
                    {
                        try
                        {
                            switch (option[1])
                            {
                                case 'X':
                                    doExtract = true;
                                    try { extractWhat = args[i + 1]; } catch { extractWhat = null; }
                                    break;
                                case 'P': doPack = true; break;
                                case 'R':
                                    doReplace = true;
                                    try {
                                        replaceWhat = args[i + 1];
                                        replaceWith = args[i + 2];
                                    } catch {
                                        replaceWhat = null;
                                        replaceWith = null;
                                        usageHelpFlags |= WRONG_USAGE;
                                    }
                                    break;
                                case 'L': bList = true; break;
                                case 'i': inFileName = args[i + 1]; break;
                                case 'o': outFileName = args[i + 1]; break;
                                case 'd': workingDir = args[i + 1]; break;
                                case 'v': bVerbose = true; break;
                                // These are packing options
                                case '-':
                                    switch (option.Substring(2))
                                    {
                                        case "codec": packCompressCodec = args[i + 1]; break;
                                        case "align": packDataAlign = args[i + 1]; break;
                                    }
                                    break;
                                case 'h': usageHelpFlags |= HELP_INFO; break;
                            }
                        }
                        catch
                        {
                            usageHelpFlags |= WRONG_USAGE;
                            break;
                        }
                    }
                }
            } else // Simple usage
            {
                inFileName = args[0];
                try { outFileName = args[1]; }
                catch { outFileName = null; }
            }

            // Set if we only wanted to list the contents
            _doList = bList && !(doExtract || doPack || doReplace) && !(isUsageSimple);

            // If we had any reason to stop, now's the time.
            // After this if-statement, it's assumed the user wanted to perform an operation.
            if ((usageHelpFlags > 0) || (inFileName == null) || (!(doExtract || doPack || doReplace || _doList) && !isUsageSimple))
            {
                PrintUsage(usageHelpFlags);
                return;
            }

    // STEP 2 - VERIFY THAT VALID OPTIONS WERE PASSED
            // (1) input file - Granted the above conditions, inFileName cannot be null at this point
            if (isUsageSimple || doExtract || doPack || doReplace || _doList)
            {
                if (File.Exists(inFileName)) // Applies to drag-n-drop uses
                {
                    if (isUsageSimple)
                    {
                        doExtract = true;
                        inFileName = (new FileInfo(inFileName)).FullName;
                    }
                }
                else if (File.Exists(workingDir + "\\" + inFileName))
                {
                    if (isUsageSimple) { doExtract = true; }
                    inFileName = workingDir + "\\" + inFileName;
                }
                else if (Directory.Exists(inFileName)) // Applies to drag-n-drop uses
                {
                    if (isUsageSimple)
                    {
                        doPack = true;
                        inFileName = Path.GetFullPath(inFileName);
                    }
                }
                else if (Directory.Exists(workingDir + "\\" + inFileName))
                {
                    if (isUsageSimple) { doPack = true; }
                    inFileName = workingDir + "\\" + inFileName;
                }
                else
                {
                    Console.WriteLine("Error: Could not find the specified input file or folder.");
                    return;
                }
            }
            // (2) output path - Preparing output directories
            if (outFileName != null)
            {
                string uriPrefix = "file:///";
                string uriString = null;
                // Cheap botch to check if it's an absolute path
                if (!((outFileName.Length > 3) && String.Equals(outFileName.Substring(1,2).Replace('\\','/'), ":/")))
                {
                    outFileName = workingDir + "\\" + outFileName;
                }
                uriString = uriPrefix + outFileName.Replace('\\', '/');
                if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
                {
                    //Console.WriteLine("DEBUG: uriString: \"" + uriString + "\"");
                    Console.WriteLine("Error: Invalid output path specified. Exiting process.");
                    return;
                }
                outFileName = outFileName.Replace('/', '\\');
            }
            // (3) packing options - compression codec, data alignment
            EnumCompressCodec compressCodec = EnumCompressCodec.CodecDpk;
            uint dataAlign = 2048;
            if (doPack || doReplace)
            {
                if (packCompressCodec != null)
                {
                    if (String.Equals(packCompressCodec, "none", StringComparison.CurrentCultureIgnoreCase))
                    {
                        compressCodec = EnumCompressCodec.CodecDpk;
                    }
                    else if (String.Equals(packCompressCodec, "layla", StringComparison.CurrentCultureIgnoreCase))
                    {
                        compressCodec = EnumCompressCodec.CodecLayla;
                    }/* Not implemented, according to CpkMaker
                    else if (String.Equals(packCompressCodec, "lzma", StringComparison.CurrentCultureIgnoreCase))
                    {
                        compressCodec = EnumCompressCodec.CodecLZMA;
                    }
                    else if (String.Equals(packCompressCodec, "relc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        compressCodec = EnumCompressCodec.CodecRELC;
                    }*/
                    else
                    {
                        Console.WriteLine("Error: Invalid packing codec specified: \"" + packCompressCodec + "\"");
                        return;
                    }
                }
                if (packDataAlign != null)
                {
                    try
                    {
                        Int32.TryParse(packDataAlign, out int tmp_da);
                        if ((tmp_da > 0) && ((tmp_da & (tmp_da - 1)) == 0))
                        {
                            dataAlign = (uint)tmp_da;
                        }
                        else
                        {
                            Console.WriteLine("Error: The specified data alignment is not a power of two!");
                            return;
                        }
                    }
                    catch { Console.WriteLine("Error: A non-number was specified for the data alignment!"); return; }
                }
            }


    // STEP 3 - NOW HERE'S WHERE THE PROCESS GETS STARTED
            CpkMaker cpkMaker = new CpkMaker();
            CFileData cpkFileData = null;
            CAsyncFile cManager = new CAsyncFile();
            Status status;
            if (doExtract || doReplace || _doList)
            {
                if (!cpkMaker.AnalyzeCpkFile(inFileName))
                {
                    Console.WriteLine("Error: AnalyzeCpkFile returned false!");
                    return;
                }
                cpkFileData = cpkMaker.FileData;
            }

            if (doExtract)
            {
                //Console.WriteLine("DEBUG: doExtract!");
                if (outFileName == null)
                {
                    outFileName = Path.GetDirectoryName(inFileName).Replace('/', '\\') + "\\" + Path.GetFileNameWithoutExtension(inFileName);
                }
                if ((extractWhat != null) && (extractWhat[0] != '-')) // If user wanted a specific file
                {
                    CFileInfo cFileInfo = cpkFileData.GetFileData(extractWhat);
                    if (cFileInfo != null)
                    {
                        outFileName += "\\" + extractWhat.Replace('/', '\\');
                        try { Directory.CreateDirectory(Path.GetDirectoryName(outFileName)); } catch { }
                        CAsyncFile cpkReader = new CAsyncFile();
                        cpkReader.ReadOpen(inFileName);
                        cpkReader.WaitForComplete();
                        CAsyncFile extWriter = new CAsyncFile();
                        extWriter.WriteOpen(outFileName, false);
                        extWriter.WaitForComplete();
                        cManager.Copy(cpkReader, extWriter, cFileInfo.Offset, cFileInfo.Filesize, CAsyncFile.CopyMode.ReadDecompress, cFileInfo.Extractsize);
                        cManager.WaitForComplete();
                        Console.WriteLine("Successfully extracted the file!");
                    } else
                    {
                        Console.WriteLine("Error: Unable to locate the specified file in the CPK \"" + extractWhat + "\"");
                        return;
                    }
                } else // Just extract everything
                {
                    try { Directory.CreateDirectory(outFileName); } catch { }
                    cpkMaker.StartToExtract(outFileName); // Continues at STEP 4
                }
            }
            else if (doPack)
            {
                //Console.WriteLine("DEBUG: doPack!");
                cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeFilename;
                cpkMaker.CompressCodec = compressCodec;
                cpkMaker.DataAlign = dataAlign;

                uint i = 0;
                string[] files = Directory.GetFiles(inFileName, "*", SearchOption.AllDirectories);
                foreach(string file in files)
                {
                    if(File.Exists(file))
                    {
                        string localpath = file.Replace(inFileName, "");
                        localpath = localpath.Replace('\\', '/');
                        if(localpath[0] == '/') { localpath = localpath.Substring(1); }
                        //Console.WriteLine("Local path = \"" + localpath + "\"");
                        cpkMaker.AddFile(file, localpath, i++, (((int)compressCodec == 1) ? false : true), "", "", dataAlign);
                    }
                }
                
                if (outFileName == null)
                {
                    outFileName = inFileName.Replace('/', '\\') + ".cpk";
                }
                
                File.Create(outFileName).Close();
                cpkMaker.StartToBuild(outFileName); // Continues at STEP 4
            }
            else if (doReplace)
            {
                //Console.WriteLine("DEBUG: doReplace!");

                CFileInfo cFileInfo = cpkFileData.GetFileData(replaceWhat);
                if (cFileInfo != null)
                {
                    string uriPrefix = "file:///";
                    string uriString = null;
                    // Cheap botch to check if it's an absolute path
                    if (!((replaceWith.Length > 3) && String.Equals(replaceWith.Substring(1, 2).Replace('\\', '/'), ":/")))
                    {
                        replaceWith = workingDir + "\\" + replaceWith;
                    }
                    uriString = uriPrefix + replaceWith.Replace('\\', '/');
                    if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
                    {
                        //Console.WriteLine("DEBUG: uriString: \"" + uriString + "\"");
                        Console.WriteLine("Error: Unable to locate the specified file to inject.");
                        return;
                    }
                    replaceWith = replaceWith.Replace('/', '\\');

                    if (outFileName == null)
                    {
                        outFileName = (workingDir.Replace('/', '\\') + "\\new_" + Path.GetFileName(inFileName));
                    }
                    if (!(String.Equals(Path.GetExtension(outFileName), ".cpk", StringComparison.OrdinalIgnoreCase)))
                    {
                        outFileName += ".cpk";
                    }
                    
                    cpkMaker.DeleteFile(replaceWhat);
                    cpkMaker.AddFile(replaceWith, replaceWhat, cFileInfo.FileId, cFileInfo.IsCompressed,
                        cFileInfo.GroupString, cFileInfo.AttributeString, cFileInfo.DataAlign);
                    cpkMaker.FileData.UpdateFileInfoPackingOrder();

                    Console.WriteLine("Preparing new CPK...");
                    File.Create(outFileName).Close();
                    cpkMaker.StartToBuild(outFileName);
                    cpkMaker.WaitForComplete();

                    CAsyncFile currOldFile = new CAsyncFile(2);
                    currOldFile.ReadOpen(inFileName);
                    currOldFile.WaitForComplete();
                    CAsyncFile patchedFile = new CAsyncFile(2);
                    patchedFile.WriteOpen(outFileName, true);
                    patchedFile.WaitForComplete();
                    Console.WriteLine("Patching in files...");

                    for (int i = 0; i < cpkMaker.FileData.FileInfos.Count; i++)
                    {
                        // I feel like I'd rather just compare CFileInfo.InfoIndex, but I'm a bit paranoid about that...
                        // At least directly comparing the content path is assured without a shadow of a doubt to tell us what we need to know
                        // I want to avoid unnecessary instance invocations of objects and complex conditionals if possible
                        // Please, for the sake of this loop, never invoke CpkMaker.FileData.UpdateFileInfoIndex()
                        //if(String.Equals(cpkFileData.FileInfos[i].ContentFilePath, cFileInfo.ContentFilePath)) { continue; }

                        CFileInfo currNewFileInfo = cpkMaker.FileData.FileInfos[i];
                        CFileInfo currOldFileInfo = cpkFileData.GetFileData(currNewFileInfo.ContentFilePath);
                        bool wasThisPatched = (currNewFileInfo.InfoIndex == cFileInfo.InfoIndex);
                        if (!wasThisPatched)  // Eh, I'll try it. 
                        {
                            patchedFile.Position = currNewFileInfo.Offset + currOldFileInfo.DataAlign;
                            //Console.WriteLine("Current position = 0x" + patchedFile.Position.ToString("X8"));
                            currOldFile.ReadAlloc(currOldFileInfo.Offset + currOldFileInfo.DataAlign, currOldFileInfo.Filesize);
                            currOldFile.WaitForComplete();
                            unsafe
                            {
                                patchedFile.Write(currOldFile.ReadBuffer, currOldFileInfo.Filesize, CAsyncFile.WriteMode.Normal);
                                patchedFile.WaitForComplete();
                            }
                            currOldFile.ReleaseBuffer();
                        }
                        if (bVerbose) { Console.WriteLine("[" + currNewFileInfo.InfoIndex.ToString().PadLeft(5) + "] " +
                                (wasThisPatched ? "PATCHED " : "  ") + currNewFileInfo.ContentFilePath + " ...");
                        }
                        //if (true) return; // Used with debugging
                    }

                    Console.WriteLine("Patch complete!");
                }
                else
                {
                    Console.WriteLine("Error: Unable to locate the specified file in the CPK \"" + replaceWhat + "\"");
                    return;
                }
            }
            else if (_doList)
            {
                //Console.WriteLine("DEBUG: doList! - TODO");
                if (bVerbose) { cpkMaker.DebugPrintInternalInfo(); }
                else { Console.WriteLine(cpkMaker.GetCpkInformationString(true, false)); }
            }
            else
            {
                Console.WriteLine("Error: Did nothing?");
                return;
            }

            // STEP 4 - PROGRESS LOOP FOR WHERE APPLICABLE (after I complete the above versions)
            if (doExtract || doPack)
            {
                int last_p = -1;
                int percent = 0;
                status = cpkMaker.Execute();
                while ((status > Status.Stop) && (percent < 100))
                {
                    percent = (int)Math.Floor(cpkMaker.GetProgress());
                    if (percent > last_p)
                    {
                        Console.CursorLeft = 0;
                        Console.Write(percent.ToString() + "% " + (doExtract ? "extracted" : "packed") + "...");
                        last_p = percent;
                    }
                    status = cpkMaker.Execute();
                }
                Console.WriteLine("");
                Console.WriteLine("Status = " + status.ToString());
            }

            Console.WriteLine("\nProcess finished (hopefully) without issues!");
        }


        static void PrintUsage(byte printFlags)
        {
            Console.WriteLine("");
            if ((printFlags & (NO_ARGS | WRONG_USAGE)) > 0)
            {
                Console.Write("Error: " + (((printFlags & NO_ARGS) > 0) ? "no arguments" : "incorrect usage") + "\n");
            }
            if ((printFlags & HELP_INFO) > 0)
            {
                Console.WriteLine("YACpkTool written by Brolijah.");
                Console.WriteLine("The purpose of this tool is only to interface with CpkMaker.dll (Copyright CRI Middleware Co)");
                Console.WriteLine("Some use cases were inspired by CriPakTools.\n");
            }
                Console.WriteLine("Simple drag-n-drop-like usage:");
                Console.WriteLine("  EXTRACT: YACpkTool.exe INPUT_CPK [OUT_FOLDER]");
                Console.WriteLine("  REPACK : YACpkTool.exe IN_FOLDER [OUT_CPK_FILE]");
            if ((printFlags & HELP_INFO) > 0)
            {
                Console.WriteLine("  Examples:");
                Console.WriteLine("    YACpkTool.exe data0.cpk                  Extracts contents of data0.cpk to folder /data0/");
                Console.WriteLine("    YACpkTool.exe data0.cpk data_out         Extracts contents of data0.cpk to folder /data_out/");
                Console.WriteLine("    YACpkTool.exe data0_out                  Packs the contents of data0_out to data0_out.cpk");
                Console.WriteLine("    YACpkTool.exe data0_out new_data.cpk     Packs the contents of data0_out to new_data.cpk");
            }
                Console.WriteLine("");
                Console.WriteLine("Technical CLI usage:");
                Console.WriteLine("  YACpkTool.exe [options] {arguments}");
                Console.WriteLine("  commands:");
                Console.WriteLine("    -X {file}   Extracts files. Optional argument: A specific file");
                Console.WriteLine("    -P          Packages a folder to a CPK.");
                Console.WriteLine("    -R {file}(in cpk) {file}(in dir)");
                Console.WriteLine("                EXPERIMENTAL! Replaces a specified file in the CPK." + (((printFlags & HELP_INFO) == 0) ? " See help for more info." : ""));
                Console.WriteLine("    -L          Lists the file contents and some basic information about the CPK.");
                Console.WriteLine("  options:");
                Console.WriteLine("    -h          Displays this help information + examples + about info");
                Console.WriteLine("    -v          Displays technical info about the running process.");
                Console.WriteLine("    -i {name}   Your input file or folder name (REQUIRED FOR ALL COMMANDS)");
                Console.WriteLine("    -o {name}   Your output file or folder name (relative or absolute)");
                Console.WriteLine("    -d {path}   Directory name. If specified, extraction and/or packaging will search here instead.");

                Console.WriteLine("");
                Console.WriteLine("  packing options (only use if you know what to do):");
                Console.WriteLine("    --codec {name}    Compression codec to use.");
                Console.WriteLine("                      Default is none. Available options: none, layla"); //, lzma, relc");
                Console.WriteLine("    --align {size}    Data alignment of the CPK.");
                Console.WriteLine("                      Default is 2048. Available options: Powers of 2 between 1 and 32768.");
            if ((printFlags & HELP_INFO) > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("  Examples:");
                Console.WriteLine("    Listing contents:");
                Console.WriteLine("      YACpkTool.exe -L -i data0.cpk");
                Console.WriteLine("      YACpkTool.exe -L -v -i data0.cpk");
                Console.WriteLine("    Extraction:");
                Console.WriteLine("      YACpkTool.exe -X fol/in/cpk/thing.bin -i \"X:\\path\\to\\cpk\\data0.cpk\"");
                Console.WriteLine("      YACpkTool.exe -X -d \"X:\\path\\to\\cpk\" -i data0.cpk");
                Console.WriteLine("      YACpkTool.exe -X -i data0.cpk");
                Console.WriteLine("    Packing:");
                Console.WriteLine("      YACpkTool.exe -P -d \"X:\\path\\to\\contents\\\" -i folder -o new_package.cpk");
                Console.WriteLine("      YACpkTool.exe -P -i \"X:\\path\\to\\contents\\folder\\\"");
                Console.WriteLine("      YACpkTool.exe -P -i folder --codec LAYLA --align 2048");
                Console.WriteLine("    Replacing:");
                Console.WriteLine("      YACpkTool.exe -d \"X:\\path\\to\\cpk\" -i data0.cpk -R fol/in/cpk/thing.bin new_thing.bin");
                Console.WriteLine("      YACpkTool.exe -i \"X:\\path\\to\\cpk\\data0.cpk\" -R fol/in/cpk/thing.bin \"X:\\path\\to\\new\\file.bin\"");
                Console.WriteLine("      YACpkTool.exe -i data0.cpk -R fol/in/cpk/thing.bin new_thing.bin -o data0_patched.cpk");
            }

        }
    }
}
