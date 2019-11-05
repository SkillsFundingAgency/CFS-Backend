Example usage:

dotnet CalculateFunding.Migrations.Calculations.Etl.dll --src-calcs-uri https://localhost:7002/api/ --src-calcs-key Local --src-specs-uri https://localhost:7001/api/ 
--src-specs-key Local --src-data-sets-uri https://localhost:7004/api/ --src-data-sets-key Local --dest-calcs-uri https://localhost:7002/api/ 
--dest-calcs-key Local --dest-specs-uri https://localhost:7001/api/ --dest-specs-key Local --dest-data-sets-uri https://localhost:7004/api/ 
--dest-data-sets-key Local --src-spec-id 8ba117cd-83e2-4ab4-83b1-a0e1724df091 --dest-spec-id 7edafd0c-28a0-49ce-af39-111147d7b43d

if you want to step through without making any writes to the destination specification (to test your settings etc.) you can add the 
--prevent-writes true command line option. This defaults to false if not supplied.