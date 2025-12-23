package com.woody.backend.infraestructure.entity;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.validation.constraints.NotBlank;

@Entity
public class Hello {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private long Id;
    
    @NotBlank(message = "This field cannot be null")
    private String hello;

    public String getHello() {
        return hello;
    }

    public void setHello(String hello) {
        this.hello = hello;
    }

    public long getId() {
        return Id;
    }

    public void setId(long id) {
        Id = id;
    }

    public Hello(long id, @NotBlank(message = "This field cannot be null") String hello) {
        Id = id;
        this.hello = hello;
    }

    public Hello(@NotBlank(message = "This field cannot be null") String hello) {
        this.hello = hello;
    }

    public Hello(){}
}
