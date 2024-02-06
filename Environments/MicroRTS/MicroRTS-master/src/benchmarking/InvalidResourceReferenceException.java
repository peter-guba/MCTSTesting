/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package benchmarking;

public class InvalidResourceReferenceException extends Exception
{
    public final String Resource;
    public final String Reference;

    public InvalidResourceReferenceException(String resource, String reference)
    {
        super("Resource {" + resource + "} referenced from {" + reference + "} not found.");
        Resource = resource;
        Reference = reference;
    }
}
