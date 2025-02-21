- Abbreviations:
	- File.CT = File.CreateText
	- File.C = File.Create
	- SW = StreamWriter
	- FS = FileStream
	
<ins>**_StreamWriter_:**</ins>

|Position|Class|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
|----|-----|-----|-----|----|-----|----|----|
|Base line|File.CT + string|1024|1293107|1|_FileOptions.SequentialScan_|0.00|00:1293548|
|1.|SW + char array|4096|1221|8192|_FileOptions.None_|-14.01|00:1111940|
|2.|File.CT + char array|1024|1221|8192|_FileOptions.None_|-13.82|00:1114435|
|3.|FileInfo + char array|32768|2442|4096|_FileOptions.SequentialScan_|-13.64|00:1116693|


> [!NOTE]
> _StreamWriter_ was only tested with the default buffer size of 4096 after comparing it with the _FileStream_ default.


<ins>**_FileStream_:**</ins>

|Position|Class|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
|----|-----|-----|-----|----|-----|----|----|
|Base line|File.C|4096|2442|4096|_FileOptions.None_|-82.13|00:0231060|
|1.|FS|16384|153|65536|_FileOptions.None_|-96.04|00:0051233|
|2.|File.C|32768|153|65536|_FileOptions.None_|-95.97|00:0052066|
|3.|File.C|65536|153|65536|_FileOptions.None_|-95.93|00:0052638|


> [!Note]
> _FileStream_ is clearly faster but finding the correct buffer & block size combinations seems rather tricky.


||byte array|%|
|----|----|----|
|File.C + FS (Buffer 4096) + 2442 Writes (Block 4096)|0231060|-82.13|
|File.C + FS (Buffer 4096) + 1221 Writes (Block 8192)|0114511|-91.14|
|File.C + FS (Buffer 4096) + 611 Writes (Block 16384)|0078872|-93.90|
|File.C + FS (Buffer 4096) + 306 Writes (Block 32768)|0073255|-94.33|
|File.C + FS (Buffer 4096) + 153 Writes (Block 65536)|0065392|-94.94|
|File.C + FS (Buffer 8192) + 2442 Writes (Block 4096)|0116835|-90.96|
|File.C + FS (Buffer 8192) + 1221 Writes (Block 8192)|0102979|-92.04|
|File.C + FS (Buffer 8192) + 611 Writes (Block 16384)|0072384|-94.40|
|File.C + FS (Buffer 8192) + 306 Writes (Block 32768)|0069706|-94.61|
|File.C + FS (Buffer 8192) + 153 Writes (Block 65536)|0058480|-95.48|
|File.C + FS (Buffer 16384) + 2442 Writes (Block 4096)|0078453|-93.93|
|File.C + FS (Buffer 16384) + 1221 Writes (Block 8192)|0076202|-94.11|
|File.C + FS (Buffer 16384) + 611 Writes (Block 16384)|0073298|-94.33|
|File.C + FS (Buffer 16384) + 306 Writes (Block 32768)|0071450|-94.47|
|File.C + FS (Buffer 16384) + 153 Writes (Block 65536)|0058735|-95.46|
|File.C + FS (Buffer 32768) + 2442 Writes (Block 4096)|0064887|-94.98|
|File.C + FS (Buffer 32768) + 1221 Writes (Block 8192)|0063793|-95.07|
|File.C + FS (Buffer 32768) + 611 Writes (Block 16384)|0062089|-95.20|
|File.C + FS (Buffer 32768) + 306 Writes (Block 32768)|0066671|-94.84|
|File.C + FS (Buffer 32768) + 153 Writes (Block 65536)|0052066|-95.97|
|File.C + FS (Buffer 65536) + 2442 Writes (Block 4096)|0057004|-95.59|
|File.C + FS (Buffer 65536) + 1221 Writes (Block 8192)|0054716|-95.77|
|File.C + FS (Buffer 65536) + 611 Writes (Block 16384)|0056519|-95.63|
|File.C + FS (Buffer 65536) + 306 Writes (Block 32768)|0065918|-94.90|
|File.C + FS (Buffer 65536) + 153 Writes (Block 65536)|0052638|-95.93|
|FS (Buffer 4096) + 2442 Writes (Block 4096)|0217797|-83.16|
|FS (Buffer 4096) + 1221 Writes (Block 8192)|0125412|-90.30|
|FS (Buffer 4096) + 611 Writes (Block 16384)|0077692|-94.00|
|FS (Buffer 4096) + 306 Writes (Block 32768)|0061831|-95.22|
|FS (Buffer 4096) + 153 Writes (Block 65536)|0054321|-95.80|
|FS (Buffer 8192) + 2442 Writes (Block 4096)|0118598|-90.83|
|FS (Buffer 8192) + 1221 Writes (Block 8192)|0106295|-91.78|
|FS (Buffer 8192) + 611 Writes (Block 16384)|0074605|-94.23|
|FS (Buffer 8192) + 306 Writes (Block 32768)|0059227|-95.42|
|FS (Buffer 8192) + 153 Writes (Block 65536)|0054556|-95.78|
|FS (Buffer 16384) + 2442 Writes (Block 4096)|0078896|-93.90|
|FS (Buffer 16384) + 1221 Writes (Block 8192)|0076517|-94.08|
|FS (Buffer 16384) + 611 Writes (Block 16384)|0069817|-94.60|
|FS (Buffer 16384) + 306 Writes (Block 32768)|0070845|-94.52|
|FS (Buffer 16384) + 153 Writes (Block 65536)|0051233|-96.04|
|FS (Buffer 32768) + 2442 Writes (Block 4096)|0064147|-95.04|
|FS (Buffer 32768) + 1221 Writes (Block 8192)|0060164|-95.38|
|FS (Buffer 32768) + 611 Writes (Block 16384)|0063628|-95.08|
|FS (Buffer 32768) + 306 Writes (Block 32768)|0067186|-94.80|
|FS (Buffer 32768) + 153 Writes (Block 65536)|0054407|-95.79|
|FS (Buffer 65536) + 2442 Writes (Block 4096)|0056377|-95.64|
|FS (Buffer 65536) + 1221 Writes (Block 8192)|0055779|-95.69|
|FS (Buffer 65536) + 611 Writes (Block 16384)|0053956|-95.83|
|FS (Buffer 65536) + 306 Writes (Block 32768)|0154740|-88.03|
|FS (Buffer 65536) + 153 Writes (Block 65536)|0064157|-95.04|