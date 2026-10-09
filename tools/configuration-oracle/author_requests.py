"""Author finite subsystem boundary inputs; this script never creates expectations."""
from pathlib import Path
import base64,json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'validation/configuration-corpus/subsystem-requests.jsonl'
b64=lambda x:base64.b64encode(x.encode() if isinstance(x,str) else x).decode()
def tag(x):
 if x is None:return {'kind':'null'}
 if isinstance(x,bool):return {'kind':'boolean','boolean':x}
 if isinstance(x,str):return {'kind':'bytes','bytesBase64':b64(x)}
 if isinstance(x,(int,float)):return {'kind':'number','number':str(x)}
 if isinstance(x,list):return {'kind':'list','items':[tag(v) for v in x]}
 return {'kind':'ordered-map','entries':[{'keyBase64':b64(k),'value':tag(v)}for k,v in x.items()]}
def files(values):return [{'path':k,'text':v}for k,v in values.items()]
def config(name,text,extras=None,**kwargs):
 return {'name':name,'mode':'file','configPath':'/project/tsconfig.json','files':files({'/project/tsconfig.json':json.dumps(text,separators=(',',':')) if isinstance(text,dict) else text,'/project/src/a.ts':'export const a=1;',**(extras or {})}),**kwargs}
rows=[]
for kind in ['list','map']:
 rows.append({'name':'declared-nil-'+kind,'mode':'json','value':{'kind':kind,'null':True},'files':[{'path':'/project/src/a.ts','text':''}]})
for mode in ['file','source']:
 r=config('override-raw-'+mode,{'compilerOptions':{'strict':False,'paths':{'base':['src/*']}},'files':['src/a.ts']})
 r['mode']=mode
 if mode=='source':r['text']=r['files'][0]['text']
 r['existingOptions']=tag({'strict':True,'target':7})
 r['existingOptionsRaw']=tag({'paths':{'override':['${configDir}/generated/*']},'plugins':[{'name':'probe'}]})
 r['configurationSequence']=['/project/tsconfig.json','/project/tsconfig.json'];rows.append(r)
rows.append(config('inheritance-array-cache-isolation',{'extends':['./base-one.json','./base-two.json'],'include':['src/**/*']},{'/project/base-one.json':'{"compilerOptions":{"strict":true,"paths":{"first":["${configDir}/one"]}},"exclude":["skip"]}','/project/base-two.json':'{"compilerOptions":{"target":"es2022","paths":{"second":["${configDir}/two"]}}','/project/sub/tsconfig.json':'{"extends":"../base-one.json","files":["../src/a.ts"]}'},configurationSequence=['/project/sub/tsconfig.json','/project/tsconfig.json','/project/sub/tsconfig.json'],fileQueries=['src/a.ts','skip/a.ts'],directoryQueries=['src','skip']))
rows.append(config('inheritance-cycle',{'extends':'./a.json'},{'/project/a.json':'{"extends":"./b.json"}','/project/b.json':'{"extends":"./a.json"}'},configurationSequence=['/project/tsconfig.json','/project/tsconfig.json']))
rows.append(config('package-extends-tsconfig-field',{'extends':'preset'},{'/project/node_modules/preset/package.json':'{"name":"preset","tsconfig":"configs/base.json"}','/project/node_modules/preset/configs/base.json':'{"compilerOptions":{"strict":true,"target":"es2021"}}'}))
rows.append(config('package-extends-directory-config',{'extends':'preset'},{'/project/node_modules/preset/package.json':'{"name":"preset"}','/project/node_modules/preset/tsconfig.json':'{"compilerOptions":{"noEmit":true}}'}))
rows.append(config('package-extends-export-conditions',{'extends':'preset/base'},{'/project/node_modules/preset/package.json':'{"name":"preset","exports":{"./base":{"require":"./require.json","default":"./default.json"}}}','/project/node_modules/preset/require.json':'{"compilerOptions":{"target":"es2020"}}','/project/node_modules/preset/default.json':'{"compilerOptions":{"target":"es2022"}}'}))
rows.append(config('package-self-config-priority',{'extends':'self/base'},{'/project/package.json':'{"name":"self","exports":{"./base":{"require":"./first.json","default":"./config.ts"}}}','/project/first.json':'{"compilerOptions":{"strict":false}}','/project/config.json':'{"compilerOptions":{"strict":true}}'}))
rows.append(config('package-import-map-config',{'extends':'#base'},{'/project/package.json':'{"imports":{"#base":"./base.json"}}','/project/base.json':'{"compilerOptions":{"target":"es2019"}}'}))
rows.append(config('package-types-versions-config',{'extends':'preset/base'},{'/project/node_modules/preset/package.json':'{"name":"preset","typesVersions":{"*":{"base":["config/base.json"]}}}','/project/node_modules/preset/config/base.json':'{"compilerOptions":{"strict":true}}'}))
rows.append(config('references-output-build-metadata',{'compilerOptions':{'composite':True,'declaration':True,'declarationMap':True,'sourceMap':True,'outDir':'dist','rootDir':'src','tsBuildInfoFile':'cache/a.tsbuildinfo'},'references':[{'path':'./other','prepend':True},{'path':'../outside/tsconfig.json'}],'files':['src/a.ts']},{'/project/other/tsconfig.json':'{"files":["b.ts"]}','/project/other/b.ts':'export {};'}))
rows.append(config('type-acquisition-jsconfig',{'typeAcquisition':{'enable':True,'include':['node','jest'],'exclude':['react'],'disableFilenameBasedTypeAcquisition':True},'include':['src']},{'/project/src/a.js':'let x=1;','/project/src/node.d.ts':'declare let x:number;'},configPath='/project/jsconfig.json') if False else {'name':'type-acquisition-jsconfig','mode':'file','configPath':'/project/jsconfig.json','files':files({'/project/jsconfig.json':'{"typeAcquisition":{"enable":true,"include":["node","jest"],"exclude":["react"],"disableFilenameBasedTypeAcquisition":true},"include":["src"]}','/project/src/a.js':'let x=1;','/project/src/node.d.ts':'declare let x:number;'})})
rows.append(config('mapper-inherited-manifest',{'extends':'./base.json'},{'/project/base.json':'{"contentMappers":[{"package":"mapper","extensions":[".vue"],"options":{"plugins":[{"name":"x"}]}}],"include":["src"]}','/project/node_modules/mapper/package.json':'{"name":"mapper","version":"1.2.3","typescript":{"contentMapper":{"exec":["node","./run.js"],"dynamicConfig":true,"compilerOptions":["target","jsx"]}}}','/project/src/component.vue':'<template/>'},existingOptions=tag({'runExternalCode':True}),fileQueries=['src/component.vue'],reloadFiles=files({'/project/src/new.vue':'','/project/src/a.ts':''})))
rows.append(config('mapper-options-located-errors',{'contentMappers':[{'package':'x','extensions':['.ts']},{'package':'a','extensions':['.vue']},{'package':'b','extensions':['.vue']}]},{'/project/node_modules/a/package.json':'{"name":"a","typescript":{"contentMapper":{"exec":["a"]}}}','/project/node_modules/b/package.json':'{"name":"b","typescript":{"contentMapper":{"exec":["b"]}}}'},existingOptions=tag({'runExternalCode':True})))
rows.append(config('glob-reload-unicode-case',{'include':['src/**/?*.ts'],'exclude':['src/.hidden/**','src/excluded/**']},{'/project/src/É.ts':'','/project/src/𐀀.ts':'','/project/src/é.ts':'','/project/src/.hidden/x.ts':'','/project/src/excluded/x.ts':''},caseSensitivity=0,fileQueries=['src/é.ts','src/.hidden/x.ts'],directoryQueries=['src','src/excluded'],reloadFiles=files({'/project/src/Z.ts':'','/project/src/é.ts':''})))
rows.append({'name':'drive-root-discovery','mode':'file','configPath':'C:/work/tsconfig.json','files':files({'C:/work/tsconfig.json':'{"include":["src/**/*.ts"]}','C:/work/src/a.ts':''}),'caseSensitivity':0,'directoryQueries':['src']})
rows.append({'name':'unc-root-discovery','mode':'file','configPath':'//server/share/work/tsconfig.json','files':files({'//server/share/work/tsconfig.json':'{"files":["a.ts"]}','//server/share/work/a.ts':''})})
rawpath=b'/project/\xffconfig.json'
rows.append({'name':'raw-byte-config-owner','mode':'source','text':'{"compilerOptions":{"strict":"wrong"},"files":["a.ts"]}','configPathBase64':b64(rawpath),'files':[{'pathBase64':b64(rawpath),'text':'{"compilerOptions":{"strict":"wrong"},"files":["a.ts"]}'},{'path':'/project/a.ts','text':''}]})
rows.append({'name':'read-failure-config','mode':'file','configPath':'/project/tsconfig.json','files':[{'path':'/project/tsconfig.json','text':'{}','readFailure':True}]})
rows.append({'name':'missing-config','mode':'file','configPath':'/project/tsconfig.json','files':[{'path':'/project/a.ts','text':''}]})
rows.append(config('discovery-symlink-cycle',{'include':['src/**/*']},{},fileQueries=['src/a.ts'])|{'files':files({'/project/tsconfig.json':'{"include":["src/**/*"]}','/project/src/a.ts':''})+[{'path':'/project/src/loop','symlink':'/project/src'}]})
rows.append({'name':'response-nested-errors','mode':'command','arguments':['@one.txt','--module','invalid','--watch'],'files':files({'/project/one.txt':'@two.txt --strict --target es2022 "src/a.ts"','/project/two.txt':'--checkers 2147483648 --paths {}'})})
rows.append({'name':'response-quotes-missing','mode':'command','arguments':['@bad.txt','@missing.txt'],'files':files({'/project/bad.txt':'--outDir "unterminated'})})
rows.append({'name':'build-complete-values','mode':'build','arguments':['--build','--clean','--dry','--verbose','--force','--stopBuildOnErrors','--builders','9223372036854775807','one','two']})
rows.append(config('source-existing-options-null-clearing',{'compilerOptions':{'paths':None,'plugins':None,'strict':None},'files':['src/a.ts']},existingOptions=tag({'strict':True,'paths':{'x':['src/*']},'plugins':[{'name':'x'}]})))
rows.append(config('inspect-supported-unchecked-settings',{'compilerOptions':{'allowJs':True,'jsx':'preserve','noEmit':False,'module':'commonjs','target':'es5'},'references':[{'path':'./other'}],'include':['src']},{'/project/src/component.tsx':'export const x=<div/>;','/project/src/script.js':'let x=1;'}))
for name,directory in [('checking-demo-valid','grouped-records'),('checking-demo-invalid','grouped-records-invalid')]:
 demo=ROOT/'examples/projects'/directory
 data={'/repo/examples/configuration/checking-base.json':(ROOT/'examples/configuration/checking-base.json').read_text()}
 data.update({'/repo/examples/projects/'+directory+'/'+p.relative_to(demo).as_posix():p.read_text() for p in sorted(demo.rglob('*.ts'))})
 path='/repo/examples/projects/'+directory+'/tsconfig.json';data[path]=(demo/'tsconfig.json').read_text()
 rows.append({'name':name,'mode':'file','configPath':path,'files':files(data)})
OUT.write_text(''.join(json.dumps(row,separators=(',',':'),ensure_ascii=True)+'\n'for row in rows))
print(len(rows))
