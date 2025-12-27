Disclamer:
 - Passing the tests on the application does NOT gurantee passing in the submit program.

Config file:
- In the config.env file, make to enter valid paths to your exe files.
- In the "TimeoutMS" field, entering 0 is used to not limit the runtime of the programs.

Important notes:
- In all tests, this application only tests "theoretically finite" strings, limited at some constant length, 
  as seen in the code. make sure to test longer strings yourself, if the exercise requires so.
- In Ex5, this application compares both exe's outputs. In addition, you have the option to provide
  a path to drmemory.exe (and 'true' int the corresponding field) in the config.env file inorder 
  to add testing of memory leaks and adress accesses. The provided application from the TA's 
  recieves errors from Dr. Memory. As such, this application simply lists the (non-zero) fields
  in the summary provided by Dr. Memory. 
  Make sure to do some testing yourself using Valgrind on the faculty's server.
- In Ex5, some tests reach the timeout limit even though they should have finished much sooner.
  this is a known issue, and will maybe be resolved later.

Contact me if you think anything is wrong/missing.
