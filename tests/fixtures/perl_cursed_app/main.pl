#!/usr/bin/perl

# RSH-PL-002
$password = "supersecret";

my $user_input = $ARGV[0];

# RSH-PL-001
system("cat $user_input");
my $output = `ls $user_input`;
