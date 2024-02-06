/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package benchmarking;

public class ResourceMissingException extends Exception
{
    public final String Resource;

    public ResourceMissingException(String resource)
    {
        super("Resource missing: {" + resource + "}");
        Resource = resource;
    }
}
